import { apiRequest } from '../../api/apiClient.ts'

export type TemplateElementType =
  | 'label'
  | 'number_input'
  | 'text_input'
  | 'dropdown'
  | 'checkbox'
  | 'calculated_field'
  | 'table'
  | 'section'

export type FieldVisibility = 'general' | 'selling' | 'cost'

export type DropdownOption = {
  value: string
  label: string
  numericValue?: number | null
}

export type TableColumn = {
  key: string
  label: string
  type: Exclude<TemplateElementType, 'label' | 'section' | 'table'>
  visibility?: FieldVisibility | null
  formula?: string | null
  options?: DropdownOption[] | null
}

export type TemplateElement = {
  id: string
  type: TemplateElementType | string
  key?: string | null
  label?: string | null
  visibility?: FieldVisibility | null
  defaultValue?: number | null
  min?: number | null
  max?: number | null
  decimalPlaces?: number | null
  defaultText?: string | null
  defaultChecked?: boolean | null
  formula?: string | null
  options?: DropdownOption[] | null
  columns?: TableColumn[] | null
}

export type TemplateDefinition = {
  schemaVersion: number
  sellingPriceFieldKey?: string | null
  elements: TemplateElement[]
}

export type TemplateValidationIssue = {
  code: string
  message: string
  elementId: string | null
  fieldKey: string | null
}

export type TemplateValidation = {
  isValid: boolean
  errors: TemplateValidationIssue[]
}

export type CalculatorTemplateSummary = {
  id: string
  name: string
  description: string | null
  isActive: boolean
  latestPublishedVersion: number | null
  draftVersion: number | null
  createdAt: string
  updatedAt: string | null
}

export type CalculatorTemplateOption = {
  id: string
  name: string
}

export type TemplateVersionSummary = {
  id: string
  versionNumber: number
  status: 'draft' | 'published' | 'retired' | string
  createdAt: string
  publishedAt: string | null
  publishedBy: string | null
}

export type CalculatorTemplateDetail = CalculatorTemplateSummary & {
  draftVersionId: string | null
  versions: TemplateVersionSummary[]
}

export type CalculatorTemplateVersion = {
  id: string
  templateId: string
  versionNumber: number
  status: string
  definition: TemplateDefinition
  createdAt: string
  createdBy: string | null
  publishedAt: string | null
  publishedBy: string | null
}

export function listCalculatorTemplates(): Promise<CalculatorTemplateSummary[]> {
  return apiRequest<CalculatorTemplateSummary[]>('/api/calculator-templates')
}

export function listActiveCalculatorTemplates(): Promise<CalculatorTemplateOption[]> {
  return apiRequest<CalculatorTemplateOption[]>('/api/calculator-templates?view=selector')
}

export function createCalculatorTemplate(input: {
  name: string
  description?: string
}): Promise<CalculatorTemplateDetail> {
  return apiRequest<CalculatorTemplateDetail>('/api/calculator-templates', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateCalculatorTemplate(
  templateId: string,
  input: { name: string; description?: string },
): Promise<CalculatorTemplateDetail> {
  return apiRequest<CalculatorTemplateDetail>(`/api/calculator-templates/${templateId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function activateCalculatorTemplate(templateId: string): Promise<CalculatorTemplateDetail> {
  return apiRequest<CalculatorTemplateDetail>(`/api/calculator-templates/${templateId}/activate`, {
    method: 'POST',
  })
}

export function deactivateCalculatorTemplate(
  templateId: string,
): Promise<CalculatorTemplateDetail> {
  return apiRequest<CalculatorTemplateDetail>(
    `/api/calculator-templates/${templateId}/deactivate`,
    {
      method: 'POST',
    },
  )
}

export function getCalculatorTemplate(templateId: string): Promise<CalculatorTemplateDetail> {
  return apiRequest<CalculatorTemplateDetail>(`/api/calculator-templates/${templateId}`)
}

export function getCalculatorTemplateVersion(
  templateId: string,
  versionId: string,
): Promise<CalculatorTemplateVersion> {
  return apiRequest<CalculatorTemplateVersion>(
    `/api/calculator-templates/${templateId}/versions/${versionId}`,
  )
}

export function createCalculatorTemplateVersion(
  templateId: string,
): Promise<CalculatorTemplateVersion> {
  return apiRequest<CalculatorTemplateVersion>(`/api/calculator-templates/${templateId}/versions`, {
    method: 'POST',
  })
}

export function saveCalculatorTemplateDraft(
  templateId: string,
  versionId: string,
  definition: TemplateDefinition,
): Promise<CalculatorTemplateVersion> {
  return apiRequest<CalculatorTemplateVersion>(
    `/api/calculator-templates/${templateId}/versions/${versionId}`,
    { method: 'PATCH', body: JSON.stringify({ definition }) },
  )
}

export function validateCalculatorTemplateVersion(
  templateId: string,
  versionId: string,
): Promise<TemplateValidation> {
  return apiRequest<TemplateValidation>(
    `/api/calculator-templates/${templateId}/versions/${versionId}/validate`,
    { method: 'POST' },
  )
}

export function publishCalculatorTemplateVersion(
  templateId: string,
  versionId: string,
): Promise<CalculatorTemplateVersion> {
  return apiRequest<CalculatorTemplateVersion>(
    `/api/calculator-templates/${templateId}/versions/${versionId}/publish`,
    { method: 'POST' },
  )
}

export const elementTypeLabels: Record<string, string> = {
  label: 'Label',
  number_input: 'Number Input',
  text_input: 'Text Input',
  dropdown: 'Dropdown',
  checkbox: 'Checkbox',
  calculated_field: 'Calculated Field',
  table: 'Table',
  section: 'Section',
}

export function elementTitle(element: TemplateElement): string {
  const type = elementTypeLabels[element.type] ?? element.type
  return element.label ? `${type}: ${element.label}` : type
}
