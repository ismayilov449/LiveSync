import { useDomainPush, type PushConnectionStatus, type PushHandler } from './useDomainPush';

export type { PushConnectionStatus };

export function useTicketsPush(
  token: string | null,
  tenantId: number | undefined,
  onPush: PushHandler,
) {
  return useDomainPush(token, tenantId, 'Ticket', 'ticket', 'ticket', onPush);
}

export function useQueuesPush(
  token: string | null,
  tenantId: number | undefined,
  onPush: PushHandler,
) {
  return useDomainPush(token, tenantId, 'Queue', 'queue', 'queue', onPush);
}
