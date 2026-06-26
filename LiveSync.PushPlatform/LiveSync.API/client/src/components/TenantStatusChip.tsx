interface TenantStatusChipProps {
  status: string;
}

function statusTone(status: string): 'live' | 'danger' | 'warn' {
  const normalized = status.toLowerCase();
  if (normalized === 'active') return 'live';
  if (normalized === 'suspended') return 'danger';
  return 'warn';
}

export function TenantStatusChip({ status }: TenantStatusChipProps) {
  return (
    <span className="status-pill" data-tone={statusTone(status)}>
      <span className="status-dot" aria-hidden />
      {status}
    </span>
  );
}
