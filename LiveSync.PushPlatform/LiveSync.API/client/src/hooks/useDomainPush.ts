import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';
import type { ChangeNotificationDto } from '../types';

interface FindAndSubscribeResponse {
  subscriptionId: string;
}

export type PushConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline';

const RENEW_INTERVAL_MS = 60 * 1000;

export type PushHandler = (notification: ChangeNotificationDto) => void | Promise<void>;

export function useDomainPush(
  token: string | null,
  tenantId: number | undefined,
  bucket: 'Ticket' | 'Queue',
  filterParameter: 'ticket' | 'queue',
  expectedBucket: 'ticket' | 'queue',
  onPush: PushHandler,
) {
  const onPushRef = useRef(onPush);
  onPushRef.current = onPush;
  const [status, setStatus] = useState<PushConnectionStatus>('offline');

  useEffect(() => {
    if (!token || tenantId == null) {
      setStatus('offline');
      return;
    }

    let connection: HubConnection | null = null;
    let subscriptionId: string | null = null;
    let renewTimer: ReturnType<typeof setInterval> | undefined;
    let disposed = false;

    const unsubscribeCurrent = async () => {
      if (!connection || !subscriptionId) return;
      const currentId = subscriptionId;
      subscriptionId = null;
      try {
        await connection.invoke('Unsubscribe', currentId);
      } catch {
        // connection may already be closed
      }
    };

    const subscribe = async () => {
      if (!connection || disposed) return;
      await unsubscribeCurrent();

      const response = await connection.invoke<FindAndSubscribeResponse>(
        'FindAndSubscribe',
        { bucket, filter: `${filterParameter}.TenantId == ${tenantId}` },
      );

      if (disposed) return;
      subscriptionId = response.subscriptionId;
      setStatus('connected');
    };

    const cleanup = async () => {
      disposed = true;
      if (renewTimer) clearInterval(renewTimer);
      await unsubscribeCurrent();
      if (connection) {
        try {
          await connection.stop();
        } catch {
          // ignore
        }
      }
    };

    const start = async () => {
      setStatus('connecting');

      connection = new HubConnectionBuilder()
        .withUrl(`/hubs/push?access_token=${encodeURIComponent(token)}`)
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();

      connection.onreconnecting(() => setStatus('reconnecting'));
      connection.onreconnected(async () => {
        if (disposed) return;
        try {
          await subscribe();
        } catch {
          setStatus('offline');
        }
      });
      connection.onclose(() => {
        if (!disposed) setStatus('offline');
      });

      connection.on('PushUpdate', (notification: ChangeNotificationDto) => {
        if (notification?.entity?.bucket !== expectedBucket) return;
        void Promise.resolve(onPushRef.current(notification));
      });

      await connection.start();
      if (disposed) return;

      await subscribe();

      renewTimer = setInterval(() => {
        if (subscriptionId && connection?.state === HubConnectionState.Connected) {
          void connection.invoke('Renew', subscriptionId);
        }
      }, RENEW_INTERVAL_MS);
    };

    const handleVisibilityChange = () => {
      if (document.visibilityState !== 'visible' || disposed) return;
      if (connection?.state === HubConnectionState.Connected) {
        void subscribe().catch(() => setStatus('offline'));
      } else if (connection?.state === HubConnectionState.Disconnected) {
        void start().catch(() => setStatus('offline'));
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);

    void start().catch(() => {
      if (!disposed) setStatus('offline');
    });

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      setStatus('offline');
      void cleanup();
    };
  }, [token, tenantId, bucket, filterParameter, expectedBucket]);

  return status;
}
