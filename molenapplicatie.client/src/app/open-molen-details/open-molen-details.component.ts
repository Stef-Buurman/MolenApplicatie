import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { MolenData } from '../../Interfaces/Models/MolenData';
import { MolenService } from '../../Services/MolenService';
import { Toasts } from '../../Utils/Toasts';
import { MolenDialogComponent } from '../dialogs/molen-dialog/molen-dialog.component';

@Component({
  selector: 'app-open-molen-details',
  standalone: false,
  templateUrl: './open-molen-details.component.html',
  styleUrl: './open-molen-details.component.scss',
})
export class OpenMolenDetailsComponent implements OnInit, OnDestroy {
  private readonly destroyed$ = new Subject<void>();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog,
    private molenService: MolenService,
    private toasts: Toasts,
  ) {}

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntil(this.destroyed$)).subscribe((params) => {
      const molenId = params.get('MolenId');

      if (!molenId) {
        this.goBack();
        return;
      }

      this.molenService.getMolenById(molenId).subscribe({
        next: (molen) => {
          this.openMolenDialog(molen);
        },
        error: (error) => {
          this.toasts.showError(
            error.message ?? 'De molen kon niet worden geladen.',
          );
          this.goBack();
        },
      });
    });
  }

  ngOnDestroy(): void {
    this.destroyed$.next();
    this.destroyed$.complete();
  }

  private openMolenDialog(molen: MolenData): void {
    const dialogRef = this.dialog.open(MolenDialogComponent, {
      data: { molenId: molen.id, molen },
      panelClass: 'molen-details',
    });

    dialogRef.afterClosed().subscribe({
      next: (nextMolenId: string | undefined) => {
        this.molenService.removeSelectedMolen();

        if (nextMolenId) {
          void this.router.navigate(['/map', nextMolenId]);
        } else {
          this.goBack();
        }
      },
    });
  }

  private goBack(): void {
    void this.router.navigate(['/map']);
  }
}
