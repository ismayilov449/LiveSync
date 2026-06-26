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

const RENEW_INTERVAL_MS = 60 * 1000;
const PUSH_DEBOUNCE_MS = 200;

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
    let pushDebounceTimer: ReturnType<typeof setTimeout> | undefined;
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
        { bucket: 'Item', filter: `item.TenantId == ${tenantId}` },
      );

      if (disposed) return;
      subscriptionId = response.subscriptionId;
      setStatus('connected');
    };

    const scheduleRefresh = () => {
      if (pushDebounceTimer) clearTimeout(pushDebounceTimer);
      pushDebounceTimer = setTimeout(() => {
        if (!disposed) onUpdateRef.current();
      }, PUSH_DEBOUNCE_MS);
    };

    const cleanup = async () => {
      disposed = true;
      if (pushDebounceTimer) clearTimeout(pushDebounceTimer);
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

      connection.on('PushUpdate', () => {
        scheduleRefresh();
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
  }, [token, tenantId]);

  return status;
}
