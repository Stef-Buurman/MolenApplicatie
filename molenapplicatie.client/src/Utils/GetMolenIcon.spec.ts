import { GetMolenIcon, GetMolenTypeIcon } from './GetMolenIcon';

describe('GetMolenIcon', () => {
  it('uses the normal windmill icon for an unknown active mill type', () => {
    expect(GetMolenTypeIcon(['Motormolen'])).toBe('windmolen');
    expect(GetMolenIcon('Werkend', ['Motormolen'])).toBe('windmolen.png');
  });

  it('keeps the disappeared state authoritative over the mill type', () => {
    expect(GetMolenIcon('Verdwenen', ['Motormolen'])).toBe(
      'windmolen_verdwenen.png',
    );
  });

  it('keeps the has-image suffix for fallback icons', () => {
    expect(GetMolenIcon('Werkend', ['Onbekend type'], true)).toBe(
      'windmolen_has_image.png',
    );
  });
});
