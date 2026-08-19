import { useState } from 'react';
import { Landmark } from 'lucide-react';
import { IconCircle } from '@/shared/components/IconCircle/IconCircle';
import { getInstitutionLogoUrl } from '../constants/connectionsConstants';

interface InstitutionLogoProps {
  institutionName: string;
}

export function InstitutionLogo({ institutionName }: InstitutionLogoProps) {
  const [hasLoadError, setHasLoadError] = useState(false);
  const logoUrl = getInstitutionLogoUrl(institutionName);

  if (!logoUrl || hasLoadError) {
    return <IconCircle icon={Landmark} tone="secondary" size="lg" />;
  }

  return (
    <span className="inline-flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-surface-card p-2 shadow-sm">
      <img
        src={logoUrl}
        alt={`Logo do ${institutionName}`}
        className="h-full w-full object-contain"
        loading="lazy"
        onError={() => setHasLoadError(true)}
      />
    </span>
  );
}
