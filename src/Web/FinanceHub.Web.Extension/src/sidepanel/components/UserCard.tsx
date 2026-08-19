import { UserRound } from 'lucide-react';
import type { DisplayIdentity } from '../../shared/security/token';

interface UserCardProps {
  identity: DisplayIdentity;
}

export function UserCard({ identity }: UserCardProps) {
  return (
    <section className="user-card">
      <div className="user-avatar" aria-hidden="true"><UserRound /></div>
      <div className="user-details">
        <strong>{identity.name}</strong>
        <span>{identity.email}</span>
      </div>
    </section>
  );
}
