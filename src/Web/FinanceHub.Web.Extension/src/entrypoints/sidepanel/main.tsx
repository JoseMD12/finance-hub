import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { SidePanelApp } from '../../sidepanel/SidePanelApp';
import '../../sidepanel/styles.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <SidePanelApp />
  </StrictMode>
);
