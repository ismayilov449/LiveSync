import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';

interface FindAndSubscribeResponse {
  subscriptionId: string;
}

export type PushConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'offline';

const RENEW_INTERVAL_MS = 4 * 60 * 1000;

export function useItemsPush(
  token: string | null,
  tenantId: number | undefined,
  onUpdate: () => void,
) {
  const onUpdateRef = useRef(onUpdate);
  onUpdateRef.current = onUpdate;
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

    const cleanup = async () => {
      disposed = true;
      if (renewTimer) clearInterval(renewTimer);
      if (connection && subscriptionId) {
        try {
          await connection.invoke('Unsubscribe', subscriptionId);
        } catch {
          // connection may already be closed
        }
      }
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
        setStatus('connected');
        if (!connection || disposed) return;
        try {
          const response = await connection.invoke<FindAndSubscribeResponse>(
            'FindAndSubscribe',
            { bucket: 'Item', filter: `item.TenantId == ${tenantId}` },
          );
          subscriptionId = response.subscriptionId;
        } catch {
          setStatus('offline');
        }
      });
      connection.onclose(() => {
        if (!disposed) setStatus('offline');
      });

      connection.on('PushUpdate', () => {
        onUpdateRef.current();
      });

      await connection.start();
      if (disposed) return;

      const response = await connection.invoke<FindAndSubscribeResponse>(
        'FindAndSubscribe',
        { bucket: 'Item', filter: `item.TenantId == ${tenantId}` },
      );
      if (disposed) return;

      subscriptionId = response.subscriptionId;
      setStatus('connected');

      renewTimer = setInterval(() => {
        if (subscriptionId && connection?.state === HubConnectionState.Connected) {
          void connection.invoke('Renew', subscriptionId);
        }
      }, RENEW_INTERVAL_MS);
    };

    void start().catch(() => {
      if (!disposed) setStatus('offline');
    });

    return () => {
      setStatus('offline');
      void cleanup();
    };
  }, [token, tenantId]);

  return status;
}
