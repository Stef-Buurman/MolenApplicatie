import { SafeUrl } from "@angular/platform-browser";

export class MolenImage {
  molenTbNummer: string;
  filePath: string;
  name: string;
  canBeDeleted: boolean;
  image?: SafeUrl;

  constructor(molenTbNummer: string, filePath: string, name: string, canBeDeleted = false) {
    this.molenTbNummer = molenTbNummer;
    this.filePath = filePath;
    this.name = name;
    this.canBeDeleted = canBeDeleted;
  }
}
