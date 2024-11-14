import { HttpClient } from "@angular/common/http";
import { Component, OnInit, ViewContainerRef } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { ActivatedRoute, Router } from "@angular/router";
import { MolenImage } from "../../Class/MolenImage";
import { MolenData } from "../../Interfaces/MolenData";
import { ErrorService } from "../../Services/ErrorService";
import { MapService } from "../../Services/MapService";
import { MolenService } from "../../Services/MolenService";
import { SharedDataService } from "../../Services/SharedDataService";
import { Toasts } from "../../Utils/Toasts";
import { MolenDialogComponent } from "../molen-dialog/molen-dialog.component";


@Component({
  selector: 'app-open-molen-details',
  templateUrl: './open-molen-details.component.html',
  styleUrl: './open-molen-details.component.scss'
})
export class OpenMolenDetailsComponent implements OnInit {
  selectedTenBruggeNumber: string | undefined;
  allMolens?: MolenData[];
  constructor(private route: ActivatedRoute,
    private toasts: Toasts,
    private router: Router,
    private vcr: ViewContainerRef,
    private http: HttpClient,
    private dialog: MatDialog,
    private errors: ErrorService,
    private sharedData: SharedDataService,
    private molenService: MolenService,
    private mapService: MapService) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.selectedTenBruggeNumber = params.get('TenBruggeNumber') || '';
      if (this.selectedTenBruggeNumber) {
        this.molenService.selectedMolenTenBruggeNumber = this.selectedTenBruggeNumber;
        this.molenService.getAllMolens().subscribe({
          next: (molens: MolenData[]) => {
            this.allMolens = molens;
            var molenDetails: MolenData | undefined = molens.find(molen => molen.ten_Brugge_Nr == this.selectedTenBruggeNumber);
            if (molenDetails) {
              this.OpenMolenDialog(molenDetails)
            } else {
              this.goBack();
            }
          }
        })
      }
    });
  }

  private OpenMolenDialog(molen: MolenData): void {
    const dialogRef = this.dialog.open(MolenDialogComponent, {
      data: { molen },
      panelClass: 'molen-details'
    });

    dialogRef.afterClosed().subscribe({
      next: (MolenImages: MolenImage[]) => {
        var oldmolen = this.allMolens?.find(mol => mol.ten_Brugge_Nr === molen.ten_Brugge_Nr);
        var marker = this.mapService.markers.find(marker => marker.tenBruggeNumber === molen.ten_Brugge_Nr);
        if (oldmolen) {
          var previousHasImage: boolean = oldmolen.hasImage;
          if (MolenImages.length > 1 || (MolenImages.length == 1 && MolenImages[0].name != oldmolen.ten_Brugge_Nr)) {
            oldmolen.hasImage = true;
          }
          else {
            oldmolen.hasImage = false;
          }
          if (previousHasImage != oldmolen.hasImage) {
            if (marker) {
              marker.marker.remove();
            }
            this.molenService.getMolenFromBackend(oldmolen.ten_Brugge_Nr).subscribe({
              next: (newMolenData) => {
                if (newMolenData) this.mapService.addMarker(newMolenData);
              },
              error: (err) => {
                if (oldmolen) {
                  oldmolen.addedImages = MolenImages;
                  this.mapService.addMarker(oldmolen);
                }
              }
            });
          }
        }
        this.molenService.removeSelectedMolen();
        this.goBack();
      }
    });
  }

  goBack() {
    this.router.navigate(['/']);
  }
}
