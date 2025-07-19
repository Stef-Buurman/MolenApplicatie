import { MolenData } from "../Interfaces/Models/MolenData";

export class SavedMolens {
  LastUpdatedTimestamp: number;
  Molens!: MolenData[];

  constructor(lastUpdatedTimestamp: number, molens: MolenData[]) {
    this.LastUpdatedTimestamp = lastUpdatedTimestamp;
    this.Molens = molens;
  }
}
