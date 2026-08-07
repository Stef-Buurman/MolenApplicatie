import { MolenData } from '../api/generated/data-contracts';

export class SavedMolens {
  LastUpdatedTimestamp: number;
  Molens!: MolenData[];

  constructor(lastUpdatedTimestamp: number, molens: MolenData[]) {
    this.LastUpdatedTimestamp = lastUpdatedTimestamp;
    this.Molens = molens;
  }
}
