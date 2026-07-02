import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export interface SidebarItem {
  key: string;
  label: string;
  icon: string;
  route?: string;
  roles?: string[];
  componentName?: string;
}

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  trackByKey(_index: number, item: SidebarItem): string {
    return item.key;
  }

  @Input() items: SidebarItem[] = [];
  @Input() isOpen = true;
  @Input() title = 'Angular 20';
  @Input() subtitle = 'Default layout';
  @Input() activeKey?: string;
  @Output() closeSidebar = new EventEmitter<void>();
  @Output() toggleSidebar = new EventEmitter<void>();
  @Output() itemSelected = new EventEmitter<SidebarItem>();
}
