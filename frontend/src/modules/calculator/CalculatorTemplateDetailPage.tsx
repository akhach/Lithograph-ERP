import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/apiClient.ts'
import { messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import {
  createCalculatorTemplateVersion,
  elementTitle,
  elementTypeLabels,
  getCalculatorTemplate,
  getCalculatorTemplateVersion,
  publishCalculatorTemplateVersion,
  saveCalculatorTemplateDraft,
  validateCalculatorTemplateVersion,
  type CalculatorTemplateDetail,
  type CalculatorTemplateVersion,
  type DropdownOption,
  type FieldVisibility,
  type TableColumn,
  type TemplateDefinition,
  type TemplateElement,
  type TemplateElementType,
  type TemplateValidationIssue,
} from './calculatorTemplatesApi.ts'

const addableTypes: TemplateElementType[] = [
  'section',
  'label',
  'number_input',
  'text_input',
  'dropdown',
  'checkbox',
  'calculated_field',
  'table',
]

export function CalculatorTemplateDetailPage() {
  const auth = useAuth()
  const params = useParams()
  const templateId = params.templateId ?? ''
  const canManage = auth.hasPermission(PermissionCodes.calculatorManageTemplates)
  const canPublish = auth.hasPermission(PermissionCodes.calculatorPublishTemplates)
  const [template, setTemplate] = useState<CalculatorTemplateDetail | null>(null)
  const [versionId, setVersionId] = useState<string | null>(null)
  const [version, setVersion] = useState<CalculatorTemplateVersion | null>(null)
  const [definition, setDefinition] = useState<TemplateDefinition | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [issues, setIssues] = useState<TemplateValidationIssue[]>([])
  const [error, setError] = useState<string | null>(null)
  const [confirmPublish, setConfirmPublish] = useState(false)
  const [busy, setBusy] = useState(false)

  async function loadTemplate(preferredVersionId?: string | null) {
    const next = await getCalculatorTemplate(templateId)
    setTemplate(next)
    const chosen =
      preferredVersionId ??
      versionId ??
      next.draftVersionId ??
      next.versions[next.versions.length - 1]?.id ??
      null
    setVersionId(chosen)
    return chosen
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const next = await getCalculatorTemplate(templateId)
        if (cancelled) return
        setTemplate(next)
        setVersionId(next.draftVersionId ?? next.versions[next.versions.length - 1]?.id ?? null)
        setError(null)
      } catch (caught: unknown) {
        if (!cancelled) setError(messageOf(caught))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [templateId])

  useEffect(() => {
    if (!versionId) return
    let cancelled = false
    void (async () => {
      try {
        const next = await getCalculatorTemplateVersion(templateId, versionId)
        if (cancelled) return
        setVersion(next)
        setDefinition(next.definition)
        setSelectedId(next.definition.elements[0]?.id ?? null)
        setIssues([])
        setError(null)
      } catch (caught: unknown) {
        if (!cancelled) setError(messageOf(caught))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [templateId, versionId])

  const readOnly = !version || version.status !== 'draft' || !canManage
  const selected = definition?.elements.find((element) => element.id === selectedId) ?? null

  function replaceDefinition(next: TemplateDefinition) {
    setDefinition(next)
    setIssues([])
  }

  async function persistDraft(current: TemplateDefinition) {
    if (!version || version.status !== 'draft' || !canManage) return version
    const saved = await saveCalculatorTemplateDraft(templateId, version.id, current)
    setVersion(saved)
    setDefinition(saved.definition)
    return saved
  }

  async function validate() {
    if (!version || !definition) return
    setBusy(true)
    setError(null)
    try {
      const current = readOnly ? version : await persistDraft(definition)
      if (!current) return
      const result = await validateCalculatorTemplateVersion(templateId, current.id)
      setIssues(result.errors)
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setIssues(issuesFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  async function publish() {
    if (!version || !definition) return
    setBusy(true)
    setError(null)
    setConfirmPublish(false)
    try {
      if (!readOnly) await persistDraft(definition)
      await publishCalculatorTemplateVersion(templateId, version.id)
      const nextTemplate = await getCalculatorTemplate(templateId)
      const published = await getCalculatorTemplateVersion(templateId, version.id)
      setTemplate(nextTemplate)
      setVersion(published)
      setDefinition(published.definition)
      setIssues([])
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setIssues(issuesFrom(caught))
    } finally {
      setBusy(false)
    }
  }

  async function createVersion() {
    setBusy(true)
    setError(null)
    try {
      const created = await createCalculatorTemplateVersion(templateId)
      await loadTemplate(created.id)
      setVersionId(created.id)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Stack spacing={2}>
      <Typography variant="body2">
        <Link to="/admin/calculator-templates">Calculator templates</Link>
      </Typography>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          {template?.name ?? 'Calculator template'}
        </Typography>
        <Stack direction="row" spacing={1}>
          {template?.draftVersionId && version?.id !== template.draftVersionId ? (
            <Button onClick={() => setVersionId(template.draftVersionId)}>Open draft</Button>
          ) : null}
          {canManage && template && !template.draftVersionId ? (
            <Button disabled={busy} onClick={() => void createVersion()}>
              Create New Version
            </Button>
          ) : null}
          {!readOnly ? (
            <Button
              disabled={busy || !definition}
              onClick={() => {
                if (!definition) return
                setBusy(true)
                void persistDraft(definition)
                  .then(() => setError(null))
                  .catch((caught: unknown) => setError(messageOf(caught)))
                  .finally(() => setBusy(false))
              }}
            >
              Save
            </Button>
          ) : null}
          {version && (canManage || canPublish) ? (
            <Button disabled={busy} onClick={() => void validate()}>
              Validate
            </Button>
          ) : null}
          {canPublish && version?.status === 'draft' ? (
            <Button variant="contained" disabled={busy} onClick={() => setConfirmPublish(true)}>
              Publish
            </Button>
          ) : null}
        </Stack>
      </Stack>
      {template?.description ? (
        <Typography color="text.secondary">{template.description}</Typography>
      ) : null}
      {error ? <Alert severity="error">{error}</Alert> : null}
      {issues.length > 0 ? (
        <Alert severity="warning">
          {issues.map((issue) => (
            <div
              key={`${issue.code}-${issue.elementId ?? ''}-${issue.fieldKey ?? ''}-${issue.message}`}
            >
              {issue.fieldKey ? <strong>{issue.fieldKey}</strong> : null}
              {issue.fieldKey ? ' — ' : ''}
              {issue.message}
            </div>
          ))}
        </Alert>
      ) : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Version</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Created</TableCell>
            <TableCell>Published</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {template?.versions.map((item) => (
            <TableRow
              key={item.id}
              hover
              selected={item.id === versionId}
              onClick={() => setVersionId(item.id)}
              sx={{ cursor: 'pointer' }}
            >
              <TableCell>v{item.versionNumber}</TableCell>
              <TableCell>{statusLabel(item.status)}</TableCell>
              <TableCell>{new Date(item.createdAt).toLocaleString()}</TableCell>
              <TableCell>
                {item.publishedAt ? new Date(item.publishedAt).toLocaleString() : ''}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      {version && definition ? (
        <Stack spacing={2}>
          <Typography variant="h6" component="h3">
            v{version.versionNumber} {statusLabel(version.status)}
            {readOnly ? ' (read-only)' : ''}
          </Typography>
          <TextField
            select
            label="Selling price field"
            value={definition.sellingPriceFieldKey ?? ''}
            disabled={readOnly}
            onChange={(event) =>
              replaceDefinition({
                ...definition,
                sellingPriceFieldKey: event.target.value || null,
              })
            }
          >
            <MenuItem value="">None</MenuItem>
            {calculatedKeys(definition).map((key) => (
              <MenuItem key={key} value={key}>
                {key}
              </MenuItem>
            ))}
          </TextField>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            sx={{ alignItems: 'flex-start' }}
          >
            <Stack spacing={1} sx={{ minWidth: 280, flex: 1 }}>
              {definition.elements.map((element, index) => (
                <Stack key={element.id} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <Button
                    variant={element.id === selectedId ? 'contained' : 'text'}
                    onClick={() => setSelectedId(element.id)}
                    sx={{ justifyContent: 'flex-start', flex: 1 }}
                  >
                    {elementTitle(element)}
                  </Button>
                  {!readOnly ? (
                    <>
                      <Button
                        size="small"
                        disabled={index === 0}
                        onClick={() => replaceDefinition(moveElement(definition, element.id, -1))}
                      >
                        Up
                      </Button>
                      <Button
                        size="small"
                        disabled={index === definition.elements.length - 1}
                        onClick={() => replaceDefinition(moveElement(definition, element.id, 1))}
                      >
                        Down
                      </Button>
                      <Button
                        size="small"
                        onClick={() => {
                          replaceDefinition(removeElement(definition, element.id))
                          if (selectedId === element.id) setSelectedId(null)
                        }}
                      >
                        Remove
                      </Button>
                    </>
                  ) : null}
                </Stack>
              ))}
              {definition.elements.length === 0 ? (
                <Typography color="text.secondary">No elements yet.</Typography>
              ) : null}
              {!readOnly ? (
                <TextField
                  select
                  label="Add element"
                  value=""
                  onChange={(event) => {
                    const created = newElement(event.target.value as TemplateElementType)
                    replaceDefinition({
                      ...definition,
                      elements: [...definition.elements, created],
                    })
                    setSelectedId(created.id)
                  }}
                >
                  <MenuItem value="" disabled>
                    Choose a type
                  </MenuItem>
                  {addableTypes.map((type) => (
                    <MenuItem key={type} value={type}>
                      {elementTypeLabels[type]}
                    </MenuItem>
                  ))}
                </TextField>
              ) : null}
            </Stack>
            <Stack spacing={2} sx={{ flex: 2, width: '100%' }}>
              {selected && definition ? (
                <ElementProperties
                  element={selected}
                  readOnly={readOnly}
                  onChange={(patch) =>
                    replaceDefinition(updateElement(definition, selected.id, patch))
                  }
                />
              ) : (
                <Typography color="text.secondary">
                  Select an element to edit its properties.
                </Typography>
              )}
            </Stack>
          </Stack>
        </Stack>
      ) : null}
      <Dialog open={confirmPublish} onClose={() => setConfirmPublish(false)}>
        <DialogTitle>Publish Template Version v{version?.versionNumber}?</DialogTitle>
        <DialogContent>
          <Typography>Published versions cannot be edited.</Typography>
          <Typography>New Orders will use this version.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmPublish(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => void publish()}>
            Publish
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

function ElementProperties({
  element,
  readOnly,
  onChange,
}: {
  element: TemplateElement
  readOnly: boolean
  onChange: (patch: Partial<TemplateElement>) => void
}) {
  const valueField = isValueField(element.type)
  return (
    <Stack spacing={2}>
      <TextField label="Type" value={elementTypeLabels[element.type] ?? element.type} disabled />
      <TextField
        label="Label"
        value={element.label ?? ''}
        disabled={readOnly}
        onChange={(event) => onChange({ label: event.target.value })}
      />
      {valueField ? (
        <TextField
          label="Key"
          value={element.key ?? ''}
          disabled={readOnly}
          onChange={(event) => onChange({ key: event.target.value })}
        />
      ) : null}
      {valueField ? (
        <TextField
          select
          label="Visibility"
          value={element.visibility ?? 'general'}
          disabled={readOnly}
          onChange={(event) => onChange({ visibility: event.target.value as FieldVisibility })}
        >
          <MenuItem value="general">General</MenuItem>
          <MenuItem value="selling">Selling</MenuItem>
          <MenuItem value="cost">Cost</MenuItem>
        </TextField>
      ) : null}
      {element.type === 'number_input' ? (
        <Stack direction="row" spacing={1}>
          <NumberField
            label="Default"
            value={element.defaultValue}
            readOnly={readOnly}
            onChange={(defaultValue) => onChange({ defaultValue })}
          />
          <NumberField
            label="Min"
            value={element.min}
            readOnly={readOnly}
            onChange={(min) => onChange({ min })}
          />
          <NumberField
            label="Max"
            value={element.max}
            readOnly={readOnly}
            onChange={(max) => onChange({ max })}
          />
          <NumberField
            label="Decimal places"
            value={element.decimalPlaces}
            readOnly={readOnly}
            onChange={(decimalPlaces) =>
              onChange({ decimalPlaces: decimalPlaces === null ? null : Math.trunc(decimalPlaces) })
            }
          />
        </Stack>
      ) : null}
      {element.type === 'text_input' ? (
        <TextField
          label="Default text"
          value={element.defaultText ?? ''}
          disabled={readOnly}
          onChange={(event) => onChange({ defaultText: event.target.value })}
        />
      ) : null}
      {element.type === 'checkbox' ? (
        <TextField
          select
          label="Default"
          value={element.defaultChecked ? 'true' : 'false'}
          disabled={readOnly}
          onChange={(event) => onChange({ defaultChecked: event.target.value === 'true' })}
        >
          <MenuItem value="false">False</MenuItem>
          <MenuItem value="true">True</MenuItem>
        </TextField>
      ) : null}
      {element.type === 'calculated_field' ? (
        <TextField
          label="Formula"
          value={element.formula ?? ''}
          disabled={readOnly}
          multiline
          minRows={3}
          onChange={(event) => onChange({ formula: event.target.value })}
        />
      ) : null}
      {element.type === 'dropdown' ? (
        <OptionsEditor
          options={element.options ?? []}
          readOnly={readOnly}
          onChange={(options) => onChange({ options })}
        />
      ) : null}
      {element.type === 'table' ? (
        <ColumnsEditor
          columns={element.columns ?? []}
          readOnly={readOnly}
          onChange={(columns) => onChange({ columns })}
        />
      ) : null}
    </Stack>
  )
}

function OptionsEditor({
  options,
  readOnly,
  onChange,
}: {
  options: DropdownOption[]
  readOnly: boolean
  onChange: (options: DropdownOption[]) => void
}) {
  return (
    <Stack spacing={1}>
      <Typography variant="subtitle2">Options</Typography>
      {options.map((option, index) => (
        <Stack key={index} direction="row" spacing={1}>
          <TextField
            label="Value"
            value={option.value}
            disabled={readOnly}
            onChange={(event) =>
              onChange(
                options.map((item, itemIndex) =>
                  itemIndex === index ? { ...item, value: event.target.value } : item,
                ),
              )
            }
          />
          <TextField
            label="Label"
            value={option.label}
            disabled={readOnly}
            onChange={(event) =>
              onChange(
                options.map((item, itemIndex) =>
                  itemIndex === index ? { ...item, label: event.target.value } : item,
                ),
              )
            }
          />
          <NumberField
            label="Numeric value"
            value={option.numericValue}
            readOnly={readOnly}
            onChange={(numericValue) =>
              onChange(
                options.map((item, itemIndex) =>
                  itemIndex === index ? { ...item, numericValue } : item,
                ),
              )
            }
          />
          {!readOnly ? (
            <Button onClick={() => onChange(options.filter((_, itemIndex) => itemIndex !== index))}>
              Remove
            </Button>
          ) : null}
        </Stack>
      ))}
      {!readOnly ? (
        <Button onClick={() => onChange([...options, { value: '', label: '' }])}>Add option</Button>
      ) : null}
    </Stack>
  )
}

function ColumnsEditor({
  columns,
  readOnly,
  onChange,
}: {
  columns: TableColumn[]
  readOnly: boolean
  onChange: (columns: TableColumn[]) => void
}) {
  return (
    <Stack spacing={1}>
      <Typography variant="subtitle2">Columns</Typography>
      {columns.map((column, index) => (
        <Stack key={index} spacing={1}>
          <Stack direction="row" spacing={1}>
            <TextField
              label="Key"
              value={column.key}
              disabled={readOnly}
              onChange={(event) =>
                onChange(
                  columns.map((item, itemIndex) =>
                    itemIndex === index ? { ...item, key: event.target.value } : item,
                  ),
                )
              }
            />
            <TextField
              label="Label"
              value={column.label}
              disabled={readOnly}
              onChange={(event) =>
                onChange(
                  columns.map((item, itemIndex) =>
                    itemIndex === index ? { ...item, label: event.target.value } : item,
                  ),
                )
              }
            />
            <TextField
              select
              label="Type"
              value={column.type}
              disabled={readOnly}
              onChange={(event) => {
                const type = event.target.value as TableColumn['type']
                onChange(
                  columns.map((item, itemIndex) => {
                    if (itemIndex !== index) return item
                    if (type === 'dropdown') {
                      return {
                        ...item,
                        type,
                        formula: null,
                        options: item.options?.length ? item.options : [{ value: '', label: '' }],
                      }
                    }
                    if (type === 'calculated_field') {
                      return { ...item, type, options: null, formula: item.formula ?? '' }
                    }
                    return { ...item, type, options: null, formula: null }
                  }),
                )
              }}
            >
              <MenuItem value="number_input">Number Input</MenuItem>
              <MenuItem value="text_input">Text Input</MenuItem>
              <MenuItem value="dropdown">Dropdown</MenuItem>
              <MenuItem value="checkbox">Checkbox</MenuItem>
              <MenuItem value="calculated_field">Calculated Field</MenuItem>
            </TextField>
            <TextField
              select
              label="Visibility"
              value={column.visibility ?? 'general'}
              disabled={readOnly}
              onChange={(event) =>
                onChange(
                  columns.map((item, itemIndex) =>
                    itemIndex === index
                      ? { ...item, visibility: event.target.value as FieldVisibility }
                      : item,
                  ),
                )
              }
            >
              <MenuItem value="general">General</MenuItem>
              <MenuItem value="selling">Selling</MenuItem>
              <MenuItem value="cost">Cost</MenuItem>
            </TextField>
          </Stack>
          {column.type === 'calculated_field' ? (
            <TextField
              label="Formula"
              value={column.formula ?? ''}
              disabled={readOnly}
              onChange={(event) =>
                onChange(
                  columns.map((item, itemIndex) =>
                    itemIndex === index ? { ...item, formula: event.target.value } : item,
                  ),
                )
              }
            />
          ) : null}
          {column.type === 'dropdown' ? (
            <OptionsEditor
              options={column.options ?? []}
              readOnly={readOnly}
              onChange={(options) =>
                onChange(
                  columns.map((item, itemIndex) =>
                    itemIndex === index ? { ...item, options } : item,
                  ),
                )
              }
            />
          ) : null}
          {!readOnly ? (
            <Button onClick={() => onChange(columns.filter((_, itemIndex) => itemIndex !== index))}>
              Remove column
            </Button>
          ) : null}
        </Stack>
      ))}
      {!readOnly ? (
        <Button
          onClick={() =>
            onChange([
              ...columns,
              { key: '', label: '', type: 'number_input', visibility: 'general' },
            ])
          }
        >
          Add column
        </Button>
      ) : null}
    </Stack>
  )
}

function NumberField({
  label,
  value,
  readOnly,
  onChange,
}: {
  label: string
  value: number | null | undefined
  readOnly: boolean
  onChange: (value: number | null) => void
}) {
  return (
    <TextField
      label={label}
      type="number"
      value={value ?? ''}
      disabled={readOnly}
      onChange={(event) => {
        const text = event.target.value
        if (text === '') {
          onChange(null)
          return
        }
        const parsed = Number(text)
        if (Number.isFinite(parsed)) onChange(parsed)
      }}
    />
  )
}

function newElement(type: TemplateElementType): TemplateElement {
  const id = crypto.randomUUID()
  if (type === 'label' || type === 'section') return { id, type, label: '' }
  if (type === 'table') {
    return {
      id,
      type,
      label: 'Table',
      columns: [{ key: '', label: '', type: 'number_input', visibility: 'general' }],
    }
  }
  if (type === 'dropdown') {
    return {
      id,
      type,
      key: '',
      label: '',
      visibility: 'general',
      options: [{ value: '', label: '' }],
    }
  }
  if (type === 'calculated_field') {
    return { id, type, key: '', label: '', visibility: 'general', formula: '' }
  }
  if (type === 'checkbox') {
    return { id, type, key: '', label: '', visibility: 'general', defaultChecked: false }
  }
  return { id, type, key: '', label: '', visibility: 'general' }
}

function updateElement(
  definition: TemplateDefinition,
  id: string,
  patch: Partial<TemplateElement>,
): TemplateDefinition {
  return {
    ...definition,
    elements: definition.elements.map((element) =>
      element.id === id ? { ...element, ...patch } : element,
    ),
  }
}

function removeElement(definition: TemplateDefinition, id: string): TemplateDefinition {
  return { ...definition, elements: definition.elements.filter((element) => element.id !== id) }
}

function moveElement(
  definition: TemplateDefinition,
  id: string,
  direction: -1 | 1,
): TemplateDefinition {
  const index = definition.elements.findIndex((element) => element.id === id)
  const target = index + direction
  if (index < 0 || target < 0 || target >= definition.elements.length) return definition
  const elements = [...definition.elements]
  const [item] = elements.splice(index, 1)
  elements.splice(target, 0, item)
  return { ...definition, elements }
}

function calculatedKeys(definition: TemplateDefinition): string[] {
  const keys: string[] = []
  for (const element of definition.elements) {
    if (element.type === 'calculated_field' && element.key) keys.push(element.key)
    for (const column of element.columns ?? []) {
      if (column.type === 'calculated_field' && column.key) keys.push(column.key)
    }
  }
  return keys
}

function isValueField(type: string): boolean {
  return (
    type === 'number_input' ||
    type === 'text_input' ||
    type === 'dropdown' ||
    type === 'checkbox' ||
    type === 'calculated_field'
  )
}

function statusLabel(status: string): string {
  if (status === 'draft') return 'Draft'
  if (status === 'published') return 'Published'
  if (status === 'retired') return 'Retired'
  return status
}

function issuesFrom(error: unknown): TemplateValidationIssue[] {
  if (!(error instanceof ApiError) || !error.validation) return []
  return error.validation
}
