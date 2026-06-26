import { describe, expect, it } from 'vitest';
import type { Ticket } from '../types';
import {
  PushOperation,
  applyDeletePatch,
  applyUpsertPatch,
  parseEntityNumericId,
  sortTicketsNewestFirst,
} from './pushListPatch';

function ticket(id: number, createdAtUtc: string, subject = `Ticket ${id}`): Ticket {
  return {
    id,
    tenantId: 1,
    queueId: 1,
    subject,
    description: '',
    status: 0,
    priority: 1,
    reporterUserId: 1,
    assigneeUserId: null,
    isActive: true,
    createdAtUtc,
    updatedAtUtc: createdAtUtc,
    comments: [],
  };
}

describe('parseEntityNumericId', () => {
  it('parses suffix from frontend entity id', () => {
    expect(parseEntityNumericId('ticket-42')).toBe(42);
    expect(parseEntityNumericId('queue-7')).toBe(7);
    expect(parseEntityNumericId('invalid')).toBeNull();
  });
});

describe('applyDeletePatch', () => {
  it('removes row and decrements total when present', () => {
    const rows = [ticket(1, '2026-01-01'), ticket(2, '2026-01-02')];
    const result = applyDeletePatch(rows, 2, 1);
    expect(result.patched).toBe(true);
    expect(result.items).toHaveLength(1);
    expect(result.items[0].id).toBe(2);
    expect(result.totalCount).toBe(1);
  });

  it('no-ops when row not on current page', () => {
    const rows = [ticket(2, '2026-01-02')];
    const result = applyDeletePatch(rows, 1, 99);
    expect(result.patched).toBe(false);
    expect(result.items).toHaveLength(1);
    expect(result.totalCount).toBe(1);
  });
});

describe('applyUpsertPatch', () => {
  it('updates existing row in place', () => {
    const rows = [ticket(1, '2026-01-01', 'Old')];
    const updated = ticket(1, '2026-01-01', 'New');
    const result = applyUpsertPatch(rows, 1, 1, 20, updated, sortTicketsNewestFirst);
    expect(result.patched).toBe(true);
    expect(result.items[0].subject).toBe('New');
    expect(result.totalCount).toBe(1);
  });

  it('inserts on page 1 sorted newest first', () => {
    const rows = [ticket(1, '2026-01-01')];
    const newer = ticket(2, '2026-06-01');
    const result = applyUpsertPatch(rows, 1, 1, 20, newer, sortTicketsNewestFirst);
    expect(result.patched).toBe(true);
    expect(result.items[0].id).toBe(2);
    expect(result.totalCount).toBe(2);
  });

  it('bumps total only when new row off page 1', () => {
    const rows = [ticket(1, '2026-01-01')];
    const newer = ticket(2, '2026-06-01');
    const result = applyUpsertPatch(rows, 1, 2, 20, newer, sortTicketsNewestFirst);
    expect(result.patched).toBe(false);
    expect(result.items).toHaveLength(1);
    expect(result.totalCount).toBe(2);
  });
});

describe('PushOperation', () => {
  it('matches server enum values', () => {
    expect(PushOperation.Upsert).toBe(1);
    expect(PushOperation.Delete).toBe(2);
  });
});
