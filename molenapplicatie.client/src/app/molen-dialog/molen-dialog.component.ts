import { HttpClient } from '@angular/common/http';
import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MolenDataClass } from '../../Class/MolenDataClass';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-molen-dialog',
  templateUrl: './molen-dialog.component.html',
  styleUrl: './molen-dialog.component.css'
})
export class MolenDialogComponent {
  public molen?: MolenDataClass;
  constructor(
    private sanitizer: DomSanitizer,
    private http: HttpClient,
    private dialogRef: MatDialogRef<MolenDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { tenBruggeNr: string }
  ) { }

  ngOnInit(): void {
    this.http.get<MolenDataClass>('/api/molen/' + this.data.tenBruggeNr).subscribe(
      (result) => {
        this.molen = result;
      },
      (error) => {
        console.error(error);
      }
    );
  }

  onClose(): void {
    this.dialogRef.close();
  }

  getImage(data: Uint8Array): any
  {
    let objectURL = 'data:image/png;base64,' + data;
    return this.sanitizer.bypassSecurityTrustUrl(objectURL);
  }
}
