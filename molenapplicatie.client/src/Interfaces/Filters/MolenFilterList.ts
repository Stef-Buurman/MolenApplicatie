export interface MolenFilterList {
  provincies: ValueName[];
  toestanden: ValueName[];
  types: ValueName[];
}

export interface ValueName {
  name: string;
  count: number;
}
