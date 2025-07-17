import { MolenData } from "./MolenData";
import { MolenType } from "./MolenType";

export interface MolenTypeAssociation {
  molenDataId: number;
  molenTypeId: number;
  molenData?: MolenData;
  molenType?: MolenType;
}
