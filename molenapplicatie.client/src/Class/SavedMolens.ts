import { MolenData } from "../Interfaces/MolenData";
import { Place } from "../Interfaces/Place";

export class SavedMolens {
  LastUpdatedTimestamp: number;
  Molens!: MolenData[];

  constructor(lastUpdatedTimestamp: number, molens: MolenData[]) {
    this.LastUpdatedTimestamp = lastUpdatedTimestamp;
    this.Molens = molens;
  }
}
