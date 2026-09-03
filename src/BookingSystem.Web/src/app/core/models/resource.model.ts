export interface ResourceResponse {
  id: number;
  name: string;
}

export interface SlotResponse {
  id: number;
  startTime: string;
  endTime: string;
}

export interface ResourceDetailResponse {
  id: number;
  name: string;
  slots: SlotResponse[];
}

export interface ResourceRequest {
  name: string;
}

export interface SlotRequest {
  startTime: string;
  endTime: string;
}
