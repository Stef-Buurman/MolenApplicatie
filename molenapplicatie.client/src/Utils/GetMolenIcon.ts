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
  }else if (types?.some((m) => m.toLowerCase() === 'tonmolen')) {
    icon = 'tonmolen';
  }else if (types?.some((m) => m.toLowerCase() === 'watermolen')) {
    icon = 'watermolen';
  } else if (types?.some((m) => m.toLowerCase() === 'rosmolen')) {
    icon = 'rosmolen';
  } else if (types?.some((m) => m.toLowerCase() === 'windmolen')) {
    icon = 'windmolen';
  }else if (types?.some((m) => m.toLowerCase() === 'verttred')) {
    icon = 'verttred';
  }else if (types?.some((m) => m.toLowerCase() === 'tjasker')) {
    icon = 'tjasker';
  }

  return icon;
}
