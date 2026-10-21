export function EmptyState({ title, description }: { title: string; description: string }) {
  return <div className="empty-state"><span className="empty-mark" aria-hidden="true">＋</span><h2>{title}</h2><p>{description}</p></div>;
}
