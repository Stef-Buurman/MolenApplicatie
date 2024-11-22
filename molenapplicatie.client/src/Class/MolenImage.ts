import { SafeUrl } from "@angular/platform-browser";

export class MolenImage {
  molenTbNummer: string;
  filePath: string;
  name: string;
  canBeDeleted: boolean;
  dateTaken: Date | undefined;

  constructor(molenTbNummer: string, filePath: string, name: string, canBeDeleted:boolean = false, dateTaken: Date | undefined = undefined) {
    this.molenTbNummer = molenTbNummer;
    this.filePath = filePath;
    this.name = name;
    this.canBeDeleted = canBeDeleted;
    this.dateTaken = dateTaken;
  }
}
