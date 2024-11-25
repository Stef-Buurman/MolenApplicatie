import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapRemainingMolensComponent } from './map-remaining-molens.component';

describe('MapRemainingMolensComponent', () => {
  let component: MapRemainingMolensComponent;
  let fixture: ComponentFixture<MapRemainingMolensComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MapRemainingMolensComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapRemainingMolensComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
