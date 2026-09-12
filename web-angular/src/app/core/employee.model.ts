export interface Employee {
  id: number;
  name: string;
  accessCode: string;
  createdAt: string;
}

export interface ImportEmployeesResult {
  created: Employee[];
  skipped: { line: number; value: string; reason: string }[];
}
