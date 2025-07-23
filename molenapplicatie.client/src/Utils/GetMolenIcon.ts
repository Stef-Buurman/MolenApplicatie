export function GetMolenIcon(
  toestand?: string,
  types?: string[],
  hasImage: boolean = false
): string {
  var icon = 'windmolen_verdwenen';

  if (toestand?.toLowerCase() == 'restant') {
    icon = 'remainder';
  } else {
    icon = GetMolenTypeIcon(types);
  }

  if (hasImage) {
    icon += '_has_image';
  }

  icon += '.png';
  return icon;
}

export function GetMolenTypeIcon(types?: string[]): string {
  var icon = 'windmolen_verdwenen';
  if (types?.some((m) => m.toLowerCase() === 'weidemolen')) {
    icon = 'weidemolen';
  } else if (types?.some((m) => m.toLowerCase() === 'paltrokmolen')) {
    icon = 'paltrokmolen';
  } else if (types?.some((m) => m.toLowerCase() === 'standerdmolen')) {
    icon = 'standerdmolen';
  } else if (
    types?.some(
      (m) => m.toLowerCase() === 'wipmolen' || m.toLowerCase() === 'spinnenkop'
    )
  ) {
    icon = 'wipmolen';
  } else if (types?.some((m) => m.toLowerCase() === 'grondzeiler')) {
    icon = 'grondzeiler';
  } else if (types?.some((m) => m.toLowerCase() === 'stellingmolen')) {
    icon = 'stellingmolen';
  } else if (types?.some((m) => m.toLowerCase() === 'beltmolen')) {
    icon = 'beltmolen';
  }
  return icon;
}
