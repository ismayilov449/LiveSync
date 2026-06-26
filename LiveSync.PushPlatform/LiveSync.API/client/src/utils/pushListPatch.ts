import type { ChangeNotificationDto, Queue, Ticket } from '../types';

export const PushOperation = {
  Upsert: 1,
  Delete: 2,
} as const;

export function parseEntityNumericId(frontendId: string): number | null {
  const match = frontendId.match(/-(\d+)$/);
  if (!match) return null;
  const id = Number(match[1]);
  return Number.isNaN(id) ? null : id;
}

export function sortTicketsNewestFirst(items: Ticket[]): Ticket[] {
  return [...items].sort((a, b) => {
    const byDate = new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
    return byDate !== 0 ? byDate : b.id - a.id;
  });
}

export function sortQueuesNewestFirst(items: Queue[]): Queue[] {
  return [...items].sort((a, b) => {
    const byDate = new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime();
    return byDate !== 0 ? byDate : b.id - a.id;
  });
}

export type PushListPatchResult<T> = {
  items: T[];
  totalCount: number;
  patched: boolean;
};

export function applyDeletePatch<T extends { id: number }>(
  items: T[],
  totalCount: number,
  entityId: number,
): PushListPatchResult<T> {
  const hadRow = items.some((row) => row.id === entityId);
  if (!hadRow) {
    return { items, totalCount, patched: false };
  }

  return {
    items: items.filter((row) => row.id !== entityId),
    totalCount: Math.max(0, totalCount - 1),
    patched: true,
  };
}

export function applyUpsertPatch<T extends { id: number; createdAtUtc: string }>(
  items: T[],
  totalCount: number,
  page: number,
  pageSize: number,
  entity: T,
  sort: (rows: T[]) => T[],
): PushListPatchResult<T> {
  const index = items.findIndex((row) => row.id === entity.id);

  if (index >= 0) {
    const next = [...items];
    next[index] = entity;
    return { items: next, totalCount, patched: true };
  }

  if (page !== 1) {
    return { items, totalCount: totalCount + 1, patched: false };
  }

  const merged = sort([entity, ...items]).slice(0, pageSize);
  return {
    items: merged,
    totalCount: totalCount + 1,
    patched: true,
  };
}

export function isPushForBucket(notification: ChangeNotificationDto, bucket: string): boolean {
  return notification?.entity?.bucket === bucket;
}
