import { Component, OnInit } from "@angular/core";
import { MatDialog } from "@angular/material/dialog";
import { ActivatedRoute, Router } from "@angular/router";
import { MolenImage } from "../../Class/MolenImage";
import { MolenData } from "../../Interfaces/MolenData";
import { MolenService } from "../../Services/MolenService";
import { MolenDialogComponent } from "../dialogs/molen-dialog/molen-dialog.component";


@Component({
  selector: 'app-open-molen-details',
  templateUrl: './open-molen-details.component.html',
  styleUrl: './open-molen-details.component.scss'
})
export class OpenMolenDetailsComponent implements OnInit {
  selectedTenBruggeNumber: string | undefined;
  constructor(private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog,
    private molenService: MolenService) { }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.selectedTenBruggeNumber = params.get('TenBruggeNumber') || '';
      if (this.selectedTenBruggeNumber) {
        this.molenService.selectedMolenTenBruggeNumber = this.selectedTenBruggeNumber;
        this.OpenMolenDialog(this.selectedTenBruggeNumber);
      }
    });
  }

  private OpenMolenDialog(tbn:string): void {
    const dialogRef = this.dialog.open(MolenDialogComponent, {
      data: { tenBruggeNr:tbn },
      panelClass: 'molen-details'
    });

    dialogRef.afterClosed().subscribe({
      next: (MolenImages: MolenImage[]) => {
        this.molenService.removeSelectedMolen();
        this.goBack();
      }
    });
  }

  goBack() {
    this.router.navigate(['../'], { relativeTo: this.route });
    //this.router.navigate(['/']);
  }
}
