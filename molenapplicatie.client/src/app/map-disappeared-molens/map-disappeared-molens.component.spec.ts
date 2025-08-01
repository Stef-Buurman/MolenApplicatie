import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapDisappearedMolensComponent } from './map-disappeared-molens.component';

describe('MapDisappearedMolensComponent', () => {
  let component: MapDisappearedMolensComponent;
  let fixture: ComponentFixture<MapDisappearedMolensComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MapDisappearedMolensComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapDisappearedMolensComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
