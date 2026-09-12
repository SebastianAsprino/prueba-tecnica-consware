export type ExpenseStatus = 'Pending' | 'Approved' | 'Rejected';

export interface ExpenseRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  amount: number;
  category: string;
  description: string;
  status: ExpenseStatus;
  autoApproved: boolean;
  rejectionReason: string | null;
  createdAt: string;
  decidedAt: string | null;
}
