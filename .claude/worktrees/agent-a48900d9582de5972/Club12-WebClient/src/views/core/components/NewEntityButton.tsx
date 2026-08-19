import React from 'react';
import { Button } from '@mui/material';
import { AddIcon } from '@/views/core/MUI/icons/icons';

interface NewEntityButtonProps {
  type: string;
  gender?: 'auto' | 'masculine' | 'feminine';
  onClick?: () => void;
  disabled?: boolean;
}

const FEMININE_WORDS = new Set([
  'division',
  'división',
  'sancion',
  'sanción',
  'puntuacion',
  'puntuación',
]);

const normalizeText = (value: string) =>
  value
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');

const resolveArticle = (
  type: string,
  gender: NewEntityButtonProps['gender']
) => {
  if (gender === 'feminine') {
    return 'Nueva';
  }

  if (gender === 'masculine') {
    return 'Nuevo';
  }

  const normalizedType = normalizeText(type);
  return FEMININE_WORDS.has(normalizedType) ? 'Nueva' : 'Nuevo';
};

const NewEntityButton: React.FC<NewEntityButtonProps> = ({
  type,
  gender = 'auto',
  onClick,
  disabled = false,
}) => {
  const article = resolveArticle(type, gender);

  return (
    <Button
      variant="contained"
      color="primary"
      startIcon={<AddIcon />}
      onClick={onClick}
      disabled={disabled}
      sx={{ mt: 0 }}
    >
      {`${article} ${type}`}
    </Button>
  );
};

export default NewEntityButton;
