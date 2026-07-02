import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { finalize } from 'rxjs';

import {
  GetLeadsQueryParams,
  LeadAssignmentItem,
  LeadFilter
} from '../../models/lead-assignment.model';
import { LEAD_CALL_RESULT_OPTIONS, LeadCallResult, SubmitLeadCallReportResponse } from '../../models/lead-report.model';
import { AuthSessionService } from '../../services/auth-session.service';
import { ConsultantStatusService } from '../../services/consultant-status.service';
import { LeadAssignmentService } from '../../services/lead-assignment.service';
import { LeadReportService } from '../../services/lead-report.service';
import { ReservationService } from '../../services/reservation.service';

@Component({
  selector: 'app-lead-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './leadManagment.component.html',
  styleUrls: ['./leadManagment.component.scss']
})
export class LeadManagmentComponent implements OnInit {
  private readonly leadAssignmentService = inject(LeadAssignmentService);
  private readonly leadReportService = inject(LeadReportService);
  private readonly reservationService = inject(ReservationService);
  private readonly consultantStatusService = inject(ConsultantStatusService);
  private readonly authSession = inject(AuthSessionService);
  private readonly toastr = inject(ToastrService);
  private readonly formBuilder = inject(FormBuilder);

  leads: LeadAssignmentItem[] = [];
  leadFilter: LeadFilter = 'all';
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  totalPages = 1;
  isLoading = false;
  isReportDialogOpen = false;
  isReservationDialogOpen = false;
  isSubmittingReport = false;
  isSubmittingReservation = false;
  selectedLead: LeadAssignmentItem | null = null;
  readonly pageSizeOptions = [5, 10, 20, 50];
  readonly callResultOptions = LEAD_CALL_RESULT_OPTIONS;

  readonly reportForm = this.formBuilder.group({
    callResult: [1 as LeadCallResult, Validators.required],
    reportDescription: ['', [Validators.required, Validators.minLength(3)]],
    patientCity: [''],
    patientRegion: [''],
    businessName: [''],
    attendanceProbabilityPercent: [null as number | null],
    secondaryPhoneNumber: ['']
  });

  readonly reservationForm = this.formBuilder.group({
    reservationAt: ['', Validators.required],
    description: ['']
  });

  ngOnInit(): void {
    this.loadLeads();
  }

  changeLeadFilter(filter: LeadFilter): void {
    this.leadFilter = filter;
    this.pageNumber = 1;
    this.loadLeads();
  }

  changeLeadPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }

    this.pageNumber = page;
    this.loadLeads();
  }

  changeLeadPageSize(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    if (!Number.isFinite(value) || value <= 0) {
      return;
    }

    this.pageSize = value;
    this.pageNumber = 1;
    this.loadLeads();
  }

  openReportDialog(lead: LeadAssignmentItem): void {
    if (this.isReportDisabled(lead)) {
      return;
    }

    this.selectedLead = lead;
    this.reportForm.reset({
      callResult: 1,
      reportDescription: '',
      patientCity: '',
      patientRegion: '',
      businessName: '',
      attendanceProbabilityPercent: null,
      secondaryPhoneNumber: ''
    });
    this.isReportDialogOpen = true;
  }

  closeReportDialog(): void {
    if (this.isSubmittingReport) {
      return;
    }

    this.isReportDialogOpen = false;
    this.selectedLead = null;
  }

  openReservationDialog(lead: LeadAssignmentItem): void {
    if (!this.canCreateReservation(lead)) {
      return;
    }

    this.selectedLead = lead;
    this.reservationForm.reset({
      reservationAt: '',
      description: ''
    });
    this.isReservationDialogOpen = true;
  }

  closeReservationDialog(): void {
    if (this.isSubmittingReservation) {
      return;
    }

    this.isReservationDialogOpen = false;
    this.selectedLead = null;
  }

  submitReport(): void {
    const profileId = this.getProfileId();
    const lead = this.selectedLead;

    if (!profileId || !lead || this.reportForm.invalid || this.isSubmittingReport) {
      this.reportForm.markAllAsTouched();
      return;
    }

    const formValue = this.reportForm.getRawValue();
    this.isSubmittingReport = true;

    this.leadReportService
      .submitReport({
        leadAssignmentId: lead.id,
        consultantProfileId: profileId,
        callResult: formValue.callResult as LeadCallResult,
        reportDescription: formValue.reportDescription.trim(),
        patientCity: formValue.patientCity?.trim() || undefined,
        patientRegion: formValue.patientRegion?.trim() || undefined,
        businessName: formValue.businessName?.trim() || undefined,
        attendanceProbabilityPercent: formValue.attendanceProbabilityPercent ?? undefined,
        secondaryPhoneNumber: formValue.secondaryPhoneNumber?.trim() || undefined
      })
      .pipe(finalize(() => (this.isSubmittingReport = false)))
      .subscribe((result) => {
        if (!result.isSuccess || !result.data) {
          this.toastr.error(result.message || 'ثبت گزارش ناموفق بود');
          return;
        }

        this.applyReportResult(lead.id, result.data);
        this.isReportDialogOpen = false;
        this.selectedLead = null;

        this.showReportSuccessFeedback(result.message, result.data);
        this.refreshDashboardStatus(profileId);
        this.loadLeads();
      });
  }

  submitReservation(): void {
    const profileId = this.getProfileId();
    const lead = this.selectedLead;

    if (!profileId || !lead || this.reservationForm.invalid || this.isSubmittingReservation) {
      this.reservationForm.markAllAsTouched();
      return;
    }

    const formValue = this.reservationForm.getRawValue();
    this.isSubmittingReservation = true;

    this.reservationService
      .createReservation({
        leadAssignmentId: lead.id,
        consultantProfileId: profileId,
        reservationAt: new Date(formValue.reservationAt).toISOString(),
        description: formValue.description?.trim() || ''
      })
      .pipe(finalize(() => (this.isSubmittingReservation = false)))
      .subscribe((result) => {
        if (!result.isSuccess) {
          this.toastr.error(result.message || 'ثبت رزرو ناموفق بود');
          return;
        }

        this.isReservationDialogOpen = false;
        this.selectedLead = null;
        this.toastr.success(result.message || 'رزرو با موفقیت ثبت شد');
        this.loadLeads();
      });
  }

  isReportDisabled(lead: LeadAssignmentItem): boolean {
    return lead.isReportSubmitted || this.isTerminalState(lead);
  }

  canCreateReservation(lead: LeadAssignmentItem): boolean {
    if (!lead.isReportSubmitted) {
      return false;
    }

    const callResult = this.normalizeCallResult(lead.callResult);
    return callResult === 'Contacted' || callResult === 'Converted' || callResult === 1 || callResult === 2;
  }

  getStatusLabel(lead: LeadAssignmentItem): string {
    switch (this.normalizeLeadAssignmentState(lead.leadAssignmentState)) {
      case 'New':
        return 'جدید';
      case 'Assigned':
        return 'اختصاص‌یافته';
      case 'Pending':
        return 'در انتظار تماس';
      case 'Contacted':
        return 'تماس موفق';
      case 'Converted':
        return 'تبدیل‌شده';
      case 'Rejected':
        return 'رد شده';
      case 'Expired':
        return 'منقضی‌شده';
      default:
        return lead.isReportSubmitted ? 'گزارش ثبت‌شده' : 'نامشخص';
    }
  }

  getStatusClass(lead: LeadAssignmentItem): string {
    switch (this.normalizeLeadAssignmentState(lead.leadAssignmentState)) {
      case 'Contacted':
      case 'Converted':
        return 'status-success';
      case 'Rejected':
      case 'Expired':
        return 'status-danger';
      case 'Assigned':
      case 'Pending':
      case 'New':
        return 'status-pending';
      default:
        return lead.isReportSubmitted ? 'status-success' : 'status-neutral';
    }
  }

  getTypeLabel(lead: LeadAssignmentItem): string {
    return this.normalizeLeadAssignmentType(lead.leadAssignmentType) === 'OfflineQueue'
      ? 'آفلاین (۲۱ تا ۹)'
      : 'لحظه‌ای';
  }

  getTypeClass(lead: LeadAssignmentItem): string {
    return this.normalizeLeadAssignmentType(lead.leadAssignmentType) === 'OfflineQueue'
      ? 'type-offline'
      : 'type-realtime';
  }

  trackByLead(_: number, lead: LeadAssignmentItem): number {
    return lead.id;
  }

  private showReportSuccessFeedback(message: string, data: SubmitLeadCallReportResponse): void {
    if (data.autoOnlineApplied) {
      this.toastr.success(message || 'گزارش با موفقیت ثبت شد و وضعیت شما به آنلاین تغییر کرد');
      return;
    }

    this.toastr.success(message || 'گزارش با موفقیت ثبت شد', undefined, { timeOut: 3500 });

    if (data.autoOnlineBlockedReason) {
      this.toastr.warning(data.autoOnlineBlockedReason, 'امکان آنلاین شدن وجود ندارد', {
        timeOut: 5000,
        closeButton: true
      });
    }
  }

  private applyReportResult(leadId: number, data: {
    isReportSubmitted: boolean;
    reportSubmittedAt: string;
    leadAssignmentState: string | number;
    callResult: LeadCallResult;
    canCreateReservation: boolean;
  }): void {
    this.leads = this.leads.map((lead) =>
      lead.id === leadId
        ? {
            ...lead,
            isReportSubmitted: data.isReportSubmitted,
            reportSubmittedAt: data.reportSubmittedAt,
            leadAssignmentState: data.leadAssignmentState,
            callResult: data.callResult
          }
        : lead
    );
  }

  private refreshDashboardStatus(profileId: number): void {
    this.consultantStatusService.refreshStatus(profileId).subscribe((result) => {
      if (!result.isSuccess || !result.data) {
        return;
      }

      this.consultantStatusService.applySnapshot(result.data);
    });
  }

  private loadLeads(): void {
    const profileId = this.getProfileId();
    if (!profileId) {
      this.toastr.error('شناسه پروفایل مشاور یافت نشد. لطفاً دوباره وارد شوید.');
      return;
    }

    this.isLoading = true;

    this.leadAssignmentService.getLeads(this.buildQuery(profileId)).subscribe((result) => {
      this.leads = [...result.items];
      this.totalCount = result.totalCount;
      this.pageNumber = result.pageNumber;
      this.pageSize = result.pageSize;
      this.totalPages = Math.max(1, result.totalPages);
      this.isLoading = false;
    });
  }

  private buildQuery(profileId: number): GetLeadsQueryParams {
    const query: GetLeadsQueryParams = {
      ProfileId: profileId,
      PageNumber: this.pageNumber,
      PageSize: this.pageSize
    };

    switch (this.leadFilter) {
      case 'new':
      case 'today':
        query.leadAssignmentState = 'New';
        break;
      case 'offlineQueue':
        query.LeadAssignmentType = 'OfflineQueue';
        break;
      case 'pending':
        query.leadAssignmentState = 'Assigned';
        break;
      case 'approved':
        query.leadAssignmentState = 'Contacted';
        break;
      case 'expired':
        query.leadAssignmentState = 'Expired';
        break;
    }

    return query;
  }

  private getProfileId(): number {
    const profileId = this.authSession.getSession()?.profileId ?? 0;
    return Number.isFinite(profileId) && profileId > 0 ? profileId : 0;
  }

  private isTerminalState(lead: LeadAssignmentItem): boolean {
    const state = this.normalizeLeadAssignmentState(lead.leadAssignmentState);
    return ['Expired', 'Rejected', 'Converted'].includes(state);
  }

  private normalizeLeadAssignmentState(state: LeadAssignmentItem['leadAssignmentState']): string {
    if (typeof state === 'string') {
      return state;
    }

    const states = ['New', 'Assigned', 'Contacted', 'Pending', 'Converted', 'Expired', 'Rejected'];
    return states[state - 1] ?? '';
  }

  private normalizeLeadAssignmentType(type: LeadAssignmentItem['leadAssignmentType']): string {
    if (typeof type === 'string') {
      return type;
    }

    const types = ['RealTime', 'OfflineQueue'];
    return types[type - 1] ?? '';
  }

  private normalizeCallResult(callResult: LeadAssignmentItem['callResult']): string | number | null {
    if (callResult === null || callResult === undefined) {
      return null;
    }

    if (typeof callResult === 'string') {
      return callResult;
    }

    const results = ['Contacted', 'Converted', 'Rejected', 'NoAnswer', 'WrongNumber', 'NeedFollowUp'];
    return results[callResult - 1] ?? callResult;
  }
}
