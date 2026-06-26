import { useCallback, useRef, useState } from 'react';

const OWN_ACTION_TTL_MS = 10_000;

export function useRemotePushHighlights() {
  const [highlightedIds, setHighlightedIds] = useState<ReadonlySet<number>>(() => new Set());
  const ownActionsRef = useRef<Map<number, number>>(new Map());

  const pruneOwnActions = () => {
    const now = Date.now();
    for (const [id, expiresAt] of ownActionsRef.current) {
      if (expiresAt <= now) ownActionsRef.current.delete(id);
    }
  };

  const markOwnAction = useCallback((id: number) => {
    ownActionsRef.current.set(id, Date.now() + OWN_ACTION_TTL_MS);
    setHighlightedIds((prev) => {
      if (!prev.has(id)) return prev;
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
  }, []);

  const noteRemotePush = useCallback((id: number) => {
    pruneOwnActions();
    if (ownActionsRef.current.has(id)) return;

    setHighlightedIds((prev) => {
      if (prev.has(id)) return prev;
      const next = new Set(prev);
      next.add(id);
      return next;
    });
  }, []);

  const clearHighlights = useCallback(() => {
    setHighlightedIds(new Set());
  }, []);

  const isHighlighted = useCallback((id: number) => highlightedIds.has(id), [highlightedIds]);

  return { markOwnAction, noteRemotePush, clearHighlights, isHighlighted };
}
