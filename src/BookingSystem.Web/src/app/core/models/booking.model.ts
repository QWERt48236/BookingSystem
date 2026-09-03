export interface BookingRequest {
  slotId: number;
  date: string;
}

export interface BookingResponse {
  id: number;
  slotId: number;
  userId: string;
  date: string;
  createdAt: string;
}

export interface AdminBookingResponse {
  id: number;
  slotId: number;
  resourceName: string;
  slotStartTime: string;
  slotEndTime: string;
  userId: string;
  userEmail: string | null;
  date: string;
  createdAt: string;
}
