export interface MessagingAlert {
  code: string;
  severity: 'critical' | 'warning';
  title: string;
  detail: string;
}
