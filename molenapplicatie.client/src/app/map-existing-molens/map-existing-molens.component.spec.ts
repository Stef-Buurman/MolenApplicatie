import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapExistingMolensComponent } from './map-existing-molens.component';

describe('MapExistingMolensComponent', () => {
  let component: MapExistingMolensComponent;
  let fixture: ComponentFixture<MapExistingMolensComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MapExistingMolensComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapExistingMolensComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
