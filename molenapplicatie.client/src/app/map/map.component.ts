import { Component, Input } from "@angular/core";

@Component({
  selector: 'app-map',
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss'
})
export class MapComponent {
  @Input() mapId!: string;

  constructor() { }
}
