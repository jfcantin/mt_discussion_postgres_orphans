# Readme
Sample repo to display orphan messages using MassTransit Postgres Transport.

## Reproduction
Taking the postgres transport for a spin and noticed I get orphan messages with 2 
queues on 1 subscription. I don't know if those 2 are related yet. I can't seem to 
reproduce the 40001 errors locally (I only get those running on Azure flexible server),
but was able to reproduce the orphans. Tried to create a failing test in MT repo but 
couldn't be sure I was wiring the test properly for the reproduction (my test was 
exiting early and leaving messages).


My deployment scenario was 2 consumers in different deployment pods and 1 subscription.
Receive mode was PartitionedOrdered. 

## Orphan messages
Repo: [repository](https://github.com/jfcantin/mt_discussion_postgres_orphans)

- Single assembly:
  - using publish.batch: after a few times I could see orphan messages
  - using publish: each time I get orphans
- Separate assemblies:
  - using publish.batch: no orphans with the current test setup
  - using publish: each time I get orphans
- In all cases I was able to confirm that all messages were consumed by the consumers, 
  for some reason not all messages get deleted from the message tables.

I did play with the receive mode and I was able to get orphans for Partitioned and 
Normal as well. 

## Concurrent serialized access error
This seem to only happen when I ran my original test on Azure flexible 
server. Locally I can't seem to reproduce yet.

**Concurrent update error**
```
Npgsql.PostgresException (0x80004005): 40001: could not serialize access due to concurrent update
at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
at Npgsql.NpgsqlDataReader.<ReadMessage>g__ReadMessageSequential|49_0(NpgsqlConnector connector, Boolean async)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
Exception data:
Severity: ERROR
SqlState: 40001
MessageText: could not serialize access due to concurrent update
Where: SQL statement "WITH msgs AS (
SELECT md.*
FROM "transport".message_delivery md
WHERE md.message_delivery_id IN (
WITH ready AS (
SELECT mdx.message_delivery_id, mdx.enqueue_time, mdx.lock_id, mdx.priority,
row_number() over ( partition by mdx.partition_key
order by mdx.priority, mdx.enqueue_time, mdx.message_delivery_id ) as row_normal,
row_number() over ( partition by mdx.partition_key
order by mdx.priority, mdx.message_delivery_id,mdx.enqueue_time ) as row_ordered,
first_value(CASE WHEN mdx.enqueue_time > v_now THEN mdx.consumer_id END) over (partition by mdx.partition_key
order by mdx.enqueue_time DESC, mdx.message_delivery_id DESC) as consumer_id,
sum(CASE WHEN mdx.enqueue_time > v_now AND mdx.consumer_id = fetch_consumer_id AND mdx.lock_id IS NOT NULL THEN 1 END)
over (partition by mdx.partition_key
order by mdx.enqueue_time DESC, mdx.message_delivery_id DESC) as active_count
FROM "transport".message_delivery mdx
WHERE mdx.queue_id = v_queue_id
AND mdx.delivery_count < mdx.max_delivery_count
)
SELECT ready.message_delivery_id
FROM ready
WHERE ( ( ordered = 0 AND ready.row_normal <= concurrent_count) OR ( ordered = 1 AND ready.row_ordered <= concurrent_count ) )
AND ready.enqueue_time <= v_now
AND (ready.consumer_id IS NULL OR ready.consumer_id = fetch_consumer_id)
AND (active_count < concurrent_count OR active_count IS NULL)
ORDER BY ready.priority, ready.enqueue_time, ready.message_delivery_id
LIMIT fetch_count FOR UPDATE SKIP LOCKED)
FOR UPDATE OF md SKIP LOCKED)
UPDATE "transport".message_delivery dm
SET delivery_count = dm.delivery_count + 1,
last_delivered = v_now,
consumer_id = fetch_consumer_id,
lock_id = fetch_lock_id,
enqueue_time = v_enqueue_time
FROM msgs
INNER JOIN "transport".message m on msgs.transport_message_id = m.transport_message_id
WHERE dm.message_delivery_id = msgs.message_delivery_id
RETURNING
dm.transport_message_id,
dm.queue_id,
dm.priority,
dm.message_delivery_id,
dm.consumer_id,
dm.lock_id,
dm.enqueue_time,
dm.expiration_time,
dm.delivery_count,
dm.partition_key,
dm.routing_key,
dm.transport_headers,
m.content_type,
m.message_type,
m.body,
m.binary_body,
m.message_id,
m.correlation_id,
m.conversation_id,
m.request_id,
m.initiator_id,
m.source_address,
m.destination_address,
m.response_address,
m.fault_address,
m.sent_time,
m.headers,
m.host"
PL/pgSQL function transport.fetch_messages_partitioned(text,uuid,uuid,interval,integer,integer,integer) line 15 at RETURN QUERY
File: nodeLockRows.c
Line: 226
Routine: ExecLockRows
```

**Concurrent delete error**
```
Npgsql.PostgresException (0x80004005): 40001: could not serialize access due to concurrent delete
at Npgsql.Internal.NpgsqlConnector.ReadMessageLong(Boolean async, DataRowLoadingMode dataRowLoadingMode, Boolean readingNotifications, Boolean isReadingPrependedMessage)
at System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder`1.StateMachineBox`1.System.Threading.Tasks.Sources.IValueTaskSource<TResult>.GetResult(Int16 token)
at Npgsql.NpgsqlDataReader.<ReadMessage>g__ReadMessageSequential|49_0(NpgsqlConnector connector, Boolean async)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlDataReader.NextResult(Boolean async, Boolean isConsuming, CancellationToken cancellationToken)
at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, CommandBehavior behavior, CancellationToken cancellationToken)
Exception data:
Severity: ERROR
SqlState: 40001
MessageText: could not serialize access due to concurrent delete
Where: SQL statement "WITH metrics AS (
DELETE FROM "transport".queue_metric_capture
WHERE queue_metric_id < COALESCE((SELECT MIN(queue_metric_id) FROM "transport".queue_metric_capture), 0) + row_limit
RETURNING *
)
INSERT INTO "transport".queue_metric (start_time, duration, queue_id, consume_count, error_count, dead_letter_count)
SELECT date_trunc('minute', m.captured),
interval '1 minute',
m.queue_id,
sum(m.consume_count),
sum(m.error_count),
sum(m.dead_letter_count)
FROM metrics m
GROUP BY date_trunc('minute', m.captured), m.queue_id
ON CONFLICT ON CONSTRAINT unique_queue_metric DO
UPDATE SET consume_count = queue_metric.consume_count + excluded.consume_count,
error_count = queue_metric.error_count + excluded.error_count,
dead_letter_count = queue_metric.dead_letter_count + excluded.dead_letter_count"
PL/pgSQL function transport.process_metrics(integer) line 4 at SQL statement
File: nodeModifyTable.c
Line: 1647
Routine: ExecDelete
```