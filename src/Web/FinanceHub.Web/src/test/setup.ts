import '@testing-library/jest-dom';
import { beforeAll } from 'vitest';

// Polyfill HTMLDialogElement for jsdom
beforeAll(() => {
  HTMLDialogElement.prototype.showModal =
    HTMLDialogElement.prototype.showModal ||
    function (this: HTMLDialogElement) {
      this.open = true;
    };
  HTMLDialogElement.prototype.close =
    HTMLDialogElement.prototype.close ||
    function (this: HTMLDialogElement) {
      this.open = false;
    };
});
