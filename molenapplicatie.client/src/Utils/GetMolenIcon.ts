export function GetMolenIcon(
  toestand?: string,
  types?: string[],
  hasImage: boolean = false,
): string {
  const normalizedToestand = toestand?.trim().toLocaleLowerCase('nl-NL');
  let icon: string;

  if (normalizedToestand === 'verdwenen') {
    icon = 'windmolen_verdwenen';
  } else if (normalizedToestand === 'restant') {
    icon = 'remainder';
  } else {
    icon = GetMolenTypeIcon(types);
  }

  if (hasImage) {
    icon += '_has_image';
  }

  return `${icon}.png`;
}

export function GetMolenTypeIcon(types?: string[]): string {
  const normalizedTypes = (types ?? [])
    .filter((type): type is string => typeof type === 'string')
    .map(normalizeMolenType)
    .filter((type) => type.length > 0);

  const hasType = (...values: string[]): boolean =>
    values.some((value) => normalizedTypes.includes(normalizeMolenType(value)));
  const hasTypeContaining = (...values: string[]): boolean =>
    normalizedTypes.some((type) =>
      values.some((value) => type.includes(normalizeMolenType(value))),
    );

  if (hasType('weidemolen', 'kleine molen')) {
    return 'weidemolen';
  }

  if (hasType('paltrokmolen', 'paltrok mill')) {
    return 'paltrokmolen';
  }

  if (hasType('standerdmolen', 'post mill')) {
    return 'standerdmolen';
  }

  if (
    hasType(
      'torenmolen',
      'ronde molen',
      'tower mill',
      'turmwindmühle',
      'turmwindmuhle',
    )
  ) {
    return 'torenmolen';
  }

  if (hasType('wipmolen', 'spinnenkop', 'hollow post mill')) {
    return 'wipmolen';
  }

  if (hasType('stellingmolen')) {
    return 'stellingmolen';
  }

  if (hasType('beltmolen')) {
    return 'beltmolen';
  }

  if (hasType('grondzeiler')) {
    return 'grondzeiler';
  }

  if (
    hasType('kantige molen', 'smock mill', 'holländermühle', 'hollandermuhle')
  ) {
    return 'grondzeiler';
  }

  if (hasType('tonmolen')) {
    return 'tonmolen';
  }

  if (
    hasType('watermolen', 'schipmolen', 'watervluchtmolen', 'getijdenmolen')
  ) {
    return 'watermolen';
  }

  if (hasType('rosmolen', 'horizontale tredmolen', 'geupel')) {
    return 'rosmolen';
  }

  if (hasType('verttred', 'verticale tredmolen', 'kraan', 'karnmolenhuisje')) {
    return 'verttred';
  }

  if (hasTypeContaining('tjasker')) {
    return 'tjasker';
  }

  if (
    normalizedTypes.some(
      (type) =>
        (type.includes('windmolen') && !type.includes('onbekend')) ||
        type.includes('windmotor'),
    )
  ) {
    return 'windmolen';
  }

  return 'windmolen_verdwenen';
}

function normalizeMolenType(value: string): string {
  return value
    .trim()
    .toLocaleLowerCase('nl-NL')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
}
