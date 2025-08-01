import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MolensRootActiveComponent } from './molens-root-active.component';

describe('MolensRootActiveComponent', () => {
  let component: MolensRootActiveComponent;
  let fixture: ComponentFixture<MolensRootActiveComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MolensRootActiveComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MolensRootActiveComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
