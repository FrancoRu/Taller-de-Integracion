export interface ITable {
	data: Array<Record<string, any>>;
	style?: {
	  height?: string | number;
	  width?: string | number;
	};
  }