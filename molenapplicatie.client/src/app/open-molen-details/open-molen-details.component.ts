import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { MolenDialogComponent } from '../dialogs/molen-dialog/molen-dialog.component';
import { MolenService } from '../../Services/MolenService';
import { MolenData } from '../../Interfaces/Models/MolenData';

@Component({
  selector: 'app-open-molen-details',
  standalone: false,
  templateUrl: './open-molen-details.component.html',
  styleUrl: './open-molen-details.component.scss',
})
export class OpenMolenDetailsComponent implements OnInit {
  selectedTenBruggeNumber: string | undefined;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog,
    private molenService: MolenService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.selectedTenBruggeNumber = params.get('TenBruggeNumber') || '';
      console.log('Selected TenBruggeNumber:', this.selectedTenBruggeNumber);
      if (this.selectedTenBruggeNumber) {
        this.molenService.selectedMolenTenBruggeNumber =
          this.selectedTenBruggeNumber;
        this.molenService.getMolenByTBN(this.selectedTenBruggeNumber).subscribe({
          next: (molen) => {
            setTimeout(() => this.OpenMolenDialog(molen));
          },
        });
      }
    });
  }

  private OpenMolenDialog(tbn: MolenData): void {
    console.log('Opening Molen Dialog for TenBruggeNumber:', tbn);
    const dialogRef = this.dialog.open(MolenDialogComponent, {
      data: { tenBruggeNr: tbn.ten_Brugge_Nr, molen: tbn },
      panelClass: 'molen-details',
    });

    dialogRef.afterClosed().subscribe({
      next: (goToMolen: string | undefined) => {
        this.molenService.removeSelectedMolen();
        if (goToMolen) {
          this.goToMolen(goToMolen);
        } else {
          this.goBack();
        }
      },
    });
  }

  goToMolen(TBN: string) {
    const currentUrl = this.router.url.split('/').slice(0, -1).join('/');
    const targetUrl = `${currentUrl}/${TBN}`;
    this.router.navigateByUrl(targetUrl);
  }

  goBack() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }
}
