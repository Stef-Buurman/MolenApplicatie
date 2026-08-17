export interface MolenFilterList {
  landen: ValueName[];
  provincies: ValueName[];
  toestanden: ValueName[];
  types: ValueName[];
}

export interface ValueName {
  name: string;
  count: number;
  parent?: string | null;
}
