import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OpenMolenDetailsComponent } from './open-molen-details.component';

describe('OpenMolenDetailsComponent', () => {
  let component: OpenMolenDetailsComponent;
  let fixture: ComponentFixture<OpenMolenDetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [OpenMolenDetailsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(OpenMolenDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
