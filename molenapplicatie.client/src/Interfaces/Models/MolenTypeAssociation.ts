import { MolenType } from './MolenType';

export interface MolenTypeAssociation {
  molenDataId: string;
  molenTypeId: string;
  molenType?: MolenType | null;
}
