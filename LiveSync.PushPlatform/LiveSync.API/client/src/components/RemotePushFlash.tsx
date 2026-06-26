type RemotePushFlashProps = {
  show: boolean;
};

export function RemotePushFlash({ show }: RemotePushFlashProps) {
  if (!show) return null;

  return (
    <span className="remote-push-flash" title="Changed in another session" aria-label="Changed in another session">
      <svg className="remote-push-flash-icon" viewBox="0 0 12 16" width="11" height="14" aria-hidden>
        <path fill="currentColor" d="M7.5 0 1 9h4.5L4.5 16 11 7H6.5L7.5 0z" />
      </svg>
    </span>
  );
}
