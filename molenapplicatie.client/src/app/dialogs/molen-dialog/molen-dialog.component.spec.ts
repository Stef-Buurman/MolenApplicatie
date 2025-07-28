import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MolenDialogComponent } from './molen-dialog.component';

describe('MolenDialogComponent', () => {
  let component: MolenDialogComponent;
  let fixture: ComponentFixture<MolenDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MolenDialogComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(MolenDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
