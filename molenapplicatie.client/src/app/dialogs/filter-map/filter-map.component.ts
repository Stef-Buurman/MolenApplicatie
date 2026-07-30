import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FilterFormValues } from '../../../Interfaces/Filters/Filter';
import { MolenFilterList } from '../../../Interfaces/Filters/MolenFilterList';
import { MolenFilters } from '../../../Interfaces/Filters/MolenFilters';
import { MolenService } from '../../../Services/MolenService';

@Component({
  selector: 'app-filter-map',
  standalone: false,
  templateUrl: './filter-map.component.html',
  styleUrl: './filter-map.component.scss',
})
export class FilterMapComponent implements OnInit {
  selectedFilter: MolenFilters = {
    provincie: '',
    toestand: 'Werkend',
    type: '',
    hasImage: '',
  };

  molenFilters: MolenFilterList = {
    provincies: [],
    toestanden: [],
    types: [],
  };

  imageOptions: { name: string }[] = [
    { name: 'Met foto' },
    { name: 'Zonder foto' },
  ];

  private filters: Record<string, FilterFormValues> = {};

  constructor(
    private dialogRef: MatDialogRef<FilterMapComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { filters: FilterFormValues[] },
    private molenService: MolenService,
  ) {}

  ngOnInit(): void {
    for (const filter of this.data.filters ?? []) {
      this.filters[filter.filterName] = { ...filter };
    }

    this.selectedFilter.provincie = this.getStringFilterValue('Provincie');
    this.selectedFilter.toestand =
      this.getStringFilterValue('MolenState') || 'Werkend';

    if (!this.filters['MolenState']) {
      this.filters['MolenState'] = this.createWerkendFilter();
    }
    this.selectedFilter.type = this.getStringFilterValue('MolenType');

    const hasImage = this.filters['HasImage']?.value;
    this.selectedFilter.hasImage =
      hasImage === true ? 'Met foto' : hasImage === false ? 'Zonder foto' : '';

    this.molenService.getAllMolenFilters().subscribe({
      next: (filters) => {
        this.molenFilters = filters;
      },
    });
  }

  onClose(filters?: FilterFormValues[]): void {
    this.dialogRef.close(filters);
  }

  filterMap(): void {
    this.setStringFilter(
      'MolenState',
      'Toestand',
      this.selectedFilter.toestand,
    );
    this.setStringFilter(
      'Provincie',
      'Provincie',
      this.selectedFilter.provincie,
    );
    this.setStringFilter('MolenType', 'Molentype', this.selectedFilter.type);

    if (this.selectedFilter.hasImage === 'Met foto') {
      this.setBooleanFilter('HasImage', 'Foto', true);
    } else if (this.selectedFilter.hasImage === 'Zonder foto') {
      this.setBooleanFilter('HasImage', 'Foto', false);
    } else {
      delete this.filters['HasImage'];
    }

    this.onClose(Object.values(this.filters));
  }

  removeFilters(): void {
    const werkendFilter = this.createWerkendFilter();

    this.selectedFilter = {
      provincie: '',
      toestand: 'Werkend',
      type: '',
      hasImage: '',
    };
    this.filters = {
      MolenState: werkendFilter,
    };
    this.onClose([werkendFilter]);
  }

  private createWerkendFilter(): FilterFormValues {
    return {
      filterName: 'MolenState',
      value: 'Werkend',
      type: 'string',
      isAList: false,
      name: 'Toestand',
    };
  }

  private getStringFilterValue(filterName: string): string {
    const value = this.filters[filterName]?.value;
    return typeof value === 'string' ? value : '';
  }

  private setStringFilter(
    filterName: string,
    name: string,
    value: string,
  ): void {
    if (!value) {
      delete this.filters[filterName];
      return;
    }

    this.filters[filterName] = {
      filterName,
      value,
      type: 'string',
      isAList: false,
      name,
    };
  }

  private setBooleanFilter(
    filterName: string,
    name: string,
    value: boolean,
  ): void {
    this.filters[filterName] = {
      filterName,
      value,
      type: 'boolean',
      isAList: false,
      name,
    };
  }
}
