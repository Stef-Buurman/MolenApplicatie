import { SafeUrl } from "@angular/platform-browser";

export class MolenImage {
  molenTbNummer: string;
  content: Uint8Array;
  name: string;
  canBeDeleted: boolean;
  image?: SafeUrl;

  constructor(molenTbNummer: string, image: Uint8Array, name: string, canBeDeleted = false) {
    this.molenTbNummer = molenTbNummer;
    this.content = image;
    this.name = name;
    this.canBeDeleted = canBeDeleted;
  }
}
