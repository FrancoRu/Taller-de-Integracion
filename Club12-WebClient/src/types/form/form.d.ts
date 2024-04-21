import React from "react";

export enum InputTypesEnum {
  Text = "text",
  Password = "password",
  Email = "email",
  Date = "date",
  Range = "range",
  Checkbox = "checkbox",
  File = "file",
  Hidden = "hidden",
  Number = "number",
  Radio = "radio",
  Select = "select",
}

export interface BaseInputJSON {
  id: string;
  name: string;
  label: string;
  type: string;
  input?: inputJSON;
  options?: SelectOption[];
  value?: string;
  class?: string;
}

export interface SelectOption {
  value: string;
  label: string;
}

interface inputJSON {
  required: boolean;
  autocomplete?: string;
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  maxlength?: number;
  minlength?: number;
}

export interface ReadJSON {
  data: BaseInputJSON[];
}

export interface IForm {
  handleSubmit: (event: React.FormEvent<HTMLFormElement>) => void;
  data: BaseInputJSON[];
  value: string;
  formRef?: React.RefObject<HTMLFormElement>;
}

export interface IInput {
  data: BaseInputJSON;
}
