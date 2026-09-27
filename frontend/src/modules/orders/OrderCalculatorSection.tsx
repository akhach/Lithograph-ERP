import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Checkbox from '@mui/material/Checkbox'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import FormControlLabel from '@mui/material/FormControlLabel'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { ApiError } from '../../api/apiClient.ts'
import { messageOf } from '../auth/apiMessages.ts'
import {
  getOrderCalculator,
  resetOrderCalculator,
  saveOrderCalculator,
  type CalculatorColumn,
  type CalculatorElement,
  type CalculatorFieldValue,
  type OrderCalculator,
} from './orderCalculatorApi.ts'
import { money } from './ordersApi.ts'

type InputValue = string | boolean

export function OrderCalculatorSection({
  orderId,
  canEdit,
  reloadKey,
  onOrderChanged,
}: {
  orderId: string
  canEdit: boolean
  reloadKey: number
  onOrderChanged: () => Promise<void>
}) {
  const [calculator, setCalculator] = useState<OrderCalculator | null>(null)
  const [inputs, setInputs] = useState<Record<string, InputValue>>({})
  const [savedInputs, setSavedInputs] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [configuredMessage, setConfiguredMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [conflict, setConflict] = useState(false)
  const [confirmClear, setConfirmClear] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      setLoading(true)
      try {
        const loaded = await getOrderCalculator(orderId)
        if (cancelled) return
        applyLoaded(loaded)
      } catch (caught: unknown) {
        if (cancelled) return
        setCalculator(null)
        setConfiguredMessage(configuredState(caught))
        setError(configuredState(caught) ? null : messageOf(caught))
      } finally {
        if (!cancelled) setLoading(false)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [orderId, reloadKey])

  function applyLoaded(loaded: OrderCalculator, savedNotice: string | null = null) {
    const next = initialInputs(loaded)
    setCalculator(loaded)
    setInputs(next)
    setSavedInputs(JSON.stringify(next))
    setFieldErrors(errorsByField(loaded.fieldErrors))
    setConfiguredMessage(null)
    setConflict(false)
    setError(loaded.calculationComplete ? null : summary(loaded))
    setNotice(savedNotice)
  }

  async function reload() {
    setSaving(true)
    setError(null)
    try {
      applyLoaded(await getOrderCalculator(orderId))
    } catch (caught: unknown) {
      setError(messageOf(caught))
    } finally {
      setSaving(false)
    }
  }

  async function save() {
    if (!calculator || conflict) return
    setSaving(true)
    setError(null)
    setNotice(null)
    setFieldErrors({})
    try {
      const saved = await saveOrderCalculator(
        orderId,
        payload(calculator, inputs),
        calculator.updatedAt,
      )
      applyLoaded(saved, saved.calculationComplete ? 'Saved' : null)
      if (!saved.calculationComplete) {
        setError(
          'Calculator values were saved. Selling Price is 0 until the calculation is complete.',
        )
      }
      await onOrderChanged()
    } catch (caught: unknown) {
      if (caught instanceof ApiError && caught.code === 'CALCULATOR_CONCURRENCY_CONFLICT') {
        setConflict(true)
        setError(caught.message)
      } else {
        setError(messageOf(caught))
        setFieldErrors(errorsFromApi(caught))
      }
    } finally {
      setSaving(false)
    }
  }

  async function clearValues() {
    if (!calculator) return
    setConfirmClear(false)
    setSaving(true)
    setError(null)
    try {
      const cleared = await resetOrderCalculator(orderId, calculator.updatedAt)
      applyLoaded(cleared, 'Saved')
      await onOrderChanged()
    } catch (caught: unknown) {
      if (caught instanceof ApiError && caught.code === 'CALCULATOR_CONCURRENCY_CONFLICT') {
        setConflict(true)
      }
      setError(messageOf(caught))
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return <Typography>Loading Calculator...</Typography>
  }

  if (configuredMessage) {
    return <Alert severity="info">{configuredMessage}</Alert>
  }

  if (!calculator) {
    return error ? <Alert severity="error">{error}</Alert> : null
  }

  const dirty = JSON.stringify(inputs) !== savedInputs
  const status = saving ? 'Saving...' : conflict ? null : dirty ? 'Unsaved changes' : notice

  return (
    <Stack spacing={2} sx={{ maxWidth: 720 }}>
      <Typography variant="body2" color="text.secondary">
        {calculator.template.name} · v{calculator.templateVersion.versionNumber}
      </Typography>
      {error ? <Alert severity={conflict ? 'warning' : 'error'}>{error}</Alert> : null}
      {status ? <Typography variant="body2">{status}</Typography> : null}
      {calculator.elements.map((element) => (
        <ElementField
          key={element.id}
          element={element}
          inputs={inputs}
          calculated={calculator.calculatedValues}
          sellingKey={calculator.sellingPriceFieldKey}
          fieldErrors={fieldErrors}
          disabled={!canEdit || saving || conflict}
          onChange={(key, value) => {
            setInputs((current) => ({ ...current, [key]: value }))
            setNotice(null)
          }}
        />
      ))}
      {calculator.sellingPrice !== null && calculator.sellingPrice !== undefined ? (
        <TextField label="Selling price" value={money(calculator.sellingPrice)} disabled />
      ) : null}
      {canEdit ? (
        <Stack direction="row" spacing={1}>
          <Button variant="contained" disabled={saving || conflict} onClick={() => void save()}>
            Save
          </Button>
          <Button
            variant="outlined"
            disabled={saving || conflict}
            onClick={() => setConfirmClear(true)}
          >
            Clear values
          </Button>
          {conflict ? (
            <Button variant="outlined" disabled={saving} onClick={() => void reload()}>
              Reload
            </Button>
          ) : null}
        </Stack>
      ) : null}
      <Dialog open={confirmClear} onClose={() => setConfirmClear(false)}>
        <DialogTitle>Reset Calculator?</DialogTitle>
        <DialogContent>
          <Typography>Entered Calculator values will be cleared.</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmClear(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => void clearValues()}>
            Clear values
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

function ElementField({
  element,
  inputs,
  calculated,
  sellingKey,
  fieldErrors,
  disabled,
  onChange,
}: {
  element: CalculatorElement
  inputs: Record<string, InputValue>
  calculated: Record<string, CalculatorFieldValue>
  sellingKey?: string | null
  fieldErrors: Record<string, string>
  disabled: boolean
  onChange: (key: string, value: InputValue) => void
}) {
  if (element.type === 'section' || element.type === 'label') {
    return (
      <Typography variant={element.type === 'section' ? 'subtitle1' : 'body1'} component="h4">
        {element.label}
      </Typography>
    )
  }

  if (element.type === 'table') {
    return (
      <Stack spacing={1}>
        {element.label ? <Typography variant="subtitle1">{element.label}</Typography> : null}
        <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: 'wrap' }}>
          {(element.columns ?? []).map((column) => (
            <ColumnField
              key={column.key}
              column={column}
              inputs={inputs}
              calculated={calculated}
              sellingKey={sellingKey}
              fieldErrors={fieldErrors}
              disabled={disabled}
              onChange={onChange}
            />
          ))}
        </Stack>
      </Stack>
    )
  }

  if (element.type === 'calculated_field' && element.key) {
    return (
      <TextField
        label={element.label ?? element.key}
        value={formatCalculated(element.key, calculated[element.key], sellingKey)}
        disabled
        error={Boolean(fieldErrors[element.key])}
        helperText={fieldErrors[element.key]}
      />
    )
  }

  if (!element.key) return null
  return (
    <InputControl
      fieldKey={element.key}
      type={element.type}
      label={element.label ?? element.key}
      value={inputs[element.key]}
      options={element.options}
      error={fieldErrors[element.key]}
      disabled={disabled}
      onChange={onChange}
    />
  )
}

function ColumnField({
  column,
  inputs,
  calculated,
  sellingKey,
  fieldErrors,
  disabled,
  onChange,
}: {
  column: CalculatorColumn
  inputs: Record<string, InputValue>
  calculated: Record<string, CalculatorFieldValue>
  sellingKey?: string | null
  fieldErrors: Record<string, string>
  disabled: boolean
  onChange: (key: string, value: InputValue) => void
}) {
  if (column.type === 'calculated_field') {
    return (
      <TextField
        label={column.label}
        value={formatCalculated(column.key, calculated[column.key], sellingKey)}
        disabled
        error={Boolean(fieldErrors[column.key])}
        helperText={fieldErrors[column.key]}
        sx={{ minWidth: 180 }}
      />
    )
  }

  return (
    <InputControl
      fieldKey={column.key}
      type={column.type}
      label={column.label}
      value={inputs[column.key]}
      options={column.options}
      error={fieldErrors[column.key]}
      disabled={disabled}
      onChange={onChange}
    />
  )
}

function InputControl({
  fieldKey,
  type,
  label,
  value,
  options,
  error,
  disabled,
  onChange,
}: {
  fieldKey: string
  type: string
  label: string
  value: InputValue | undefined
  options?: CalculatorElement['options']
  error?: string
  disabled: boolean
  onChange: (key: string, value: InputValue) => void
}) {
  if (type === 'checkbox') {
    return (
      <FormControlLabel
        control={
          <Checkbox
            checked={value === true}
            disabled={disabled}
            onChange={(event) => onChange(fieldKey, event.target.checked)}
          />
        }
        label={label}
      />
    )
  }

  if (type === 'dropdown') {
    return (
      <TextField
        select
        label={label}
        value={typeof value === 'string' ? value : ''}
        disabled={disabled}
        error={Boolean(error)}
        helperText={error}
        onChange={(event) => onChange(fieldKey, event.target.value)}
      >
        <MenuItem value="">None</MenuItem>
        {(options ?? []).map((option) => (
          <MenuItem key={option.value} value={option.value}>
            {option.label}
          </MenuItem>
        ))}
      </TextField>
    )
  }

  return (
    <TextField
      label={label}
      type={type === 'number_input' ? 'number' : 'text'}
      value={typeof value === 'string' ? value : ''}
      disabled={disabled}
      error={Boolean(error)}
      helperText={error}
      onChange={(event) => onChange(fieldKey, event.target.value)}
    />
  )
}

function initialInputs(calculator: OrderCalculator): Record<string, InputValue> {
  const inputs: Record<string, InputValue> = {}
  for (const element of calculator.elements) {
    if (element.type === 'table') {
      for (const column of element.columns ?? []) {
        if (column.type !== 'calculated_field') {
          inputs[column.key] = inputValue(
            column.type,
            calculator.values[column.key],
            null,
            null,
            null,
          )
        }
      }
      continue
    }
    if (element.key && isInput(element.type)) {
      inputs[element.key] = inputValue(
        element.type,
        calculator.values[element.key],
        element.defaultValue,
        element.defaultText,
        element.defaultChecked,
      )
    }
  }
  return inputs
}

function inputValue(
  type: string,
  stored: CalculatorFieldValue | undefined,
  defaultValue: number | null | undefined,
  defaultText: string | null | undefined,
  defaultChecked: boolean | null | undefined,
): InputValue {
  if (type === 'checkbox') {
    if (typeof stored === 'boolean') return stored
    return defaultChecked ?? false
  }
  if (type === 'number_input') {
    if (typeof stored === 'number') return String(stored)
    if (typeof defaultValue === 'number') return String(defaultValue)
    return ''
  }
  if (typeof stored === 'string') return stored
  if (type === 'text_input') return defaultText ?? ''
  return ''
}

function payload(
  calculator: OrderCalculator,
  inputs: Record<string, InputValue>,
): Record<string, CalculatorFieldValue> {
  const values: Record<string, CalculatorFieldValue> = {}
  for (const element of calculator.elements) {
    if (element.type === 'table') {
      for (const column of element.columns ?? []) {
        assignValue(values, column.key, column.type, inputs[column.key])
      }
      continue
    }
    if (element.key && isInput(element.type)) {
      assignValue(values, element.key, element.type, inputs[element.key])
    }
  }
  return values
}

function assignValue(
  values: Record<string, CalculatorFieldValue>,
  key: string,
  type: string,
  raw: InputValue | undefined,
) {
  if (type === 'checkbox') {
    values[key] = raw === true
    return
  }
  if (typeof raw !== 'string' || raw.trim() === '') return
  if (type === 'number_input') {
    values[key] = /^-?\d+(\.\d+)?$/.test(raw) ? Number(raw) : raw
    return
  }
  values[key] = raw
}

function isInput(type: string): boolean {
  return (
    type === 'number_input' || type === 'text_input' || type === 'dropdown' || type === 'checkbox'
  )
}

function formatCalculated(
  key: string,
  value: CalculatorFieldValue | undefined,
  sellingKey?: string | null,
): string {
  if (typeof value === 'number') {
    return key === sellingKey ? money(value) : String(value)
  }
  if (typeof value === 'boolean') return value ? 'Yes' : 'No'
  if (typeof value === 'string') return value
  return ''
}

function errorsByField(errors: OrderCalculator['fieldErrors']): Record<string, string> {
  const mapped: Record<string, string> = {}
  for (const error of errors) {
    if (error.fieldKey) mapped[error.fieldKey] = error.message
  }
  return mapped
}

function errorsFromApi(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.errors) return {}
  const mapped: Record<string, string> = {}
  for (const [field, messages] of Object.entries(error.errors)) {
    if (messages[0]) mapped[field] = messages[0]
  }
  return mapped
}

function summary(calculator: OrderCalculator): string | null {
  if (calculator.calculationComplete || calculator.fieldErrors.length === 0) return null
  return calculator.fieldErrors.map((error) => error.message).join(' ')
}

function configuredState(error: unknown): string | null {
  if (!(error instanceof ApiError)) return null
  if (error.code === 'CALCULATOR_NOT_CONFIGURED') {
    return 'No Calculator is configured for this Order Type.'
  }
  if (error.code === 'CALCULATOR_NO_PUBLISHED_VERSION') {
    return 'This Calculator Template has no published version.'
  }
  if (error.code === 'CALCULATOR_TEMPLATE_INACTIVE') {
    return error.message
  }
  return null
}
