import { useError } from '@/modules/error/hooks/error.hock';
import { IErrorContextProp } from '@/modules/error/type/error';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchContextProps } from '@/modules/match/type/match';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import {
  IAddPlayerSanction,
  IPlayerSanctionContextProps,
  IPlayerSanctionResponse,
} from '@/modules/playerSanction/type/playerSanction';
import { GUID_EMPTY, MatchSide } from '@/views/core/constants/const';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import {
  useTheme,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  MenuItem,
} from '@mui/material';
import React, { useState, useMemo, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { IPublicPlayerResponse } from '@/modules/player/type/player';
import { GUID } from '@/modules/core/types/types';

export const CreatePlayerSanction: React.FC = () => {
  const { match }: IMatchContextProps = useMatch();
  const { addPlayerSanction }: IPlayerSanctionContextProps =
    usePlayerSanction();
  const { errors, setMessage }: IErrorContextProp = useError();
  const navigate = useNavigate();
  const theme = useTheme();

  const [selectedTeam, setSelectedTeam] = useState<MatchSide>(MatchSide.HOME);
  const [form, setForm] = useState<IAddPlayerSanction>({
    matchId: GUID_EMPTY,
    duration: 0,
    description: '',
    issuedDate: new Date(),
    playerId: GUID_EMPTY,
  });

  if (!match) {
    navigate(RoutesNavigationViews.Home, { replace: true });
    setMessage(404, [
      'Hubo un problema al cargar el partido al cual se le debe agregar el nuevo registro de sanción.',
    ]);
    return null;
  }

  useEffect(() => {
    setForm(prev => ({
      ...prev,
      matchId: match.id,
      playerId: GUID_EMPTY,
    }));
  }, [match]);

  const availablePlayers = useMemo<IPublicPlayerResponse[]>(() => {
    if (selectedTeam === MatchSide.HOME) return match.homeTeam?.players ?? [];
    if (selectedTeam === MatchSide.VISITOR)
      return match.visitorTeam?.players ?? [];
    return [];
  }, [selectedTeam, match]);

  const handleCreate = async () => {
    const message: string[] = [];

    form.duration < 1 && message.push('La duración es un campo obligatorio');
    form.playerId === GUID_EMPTY &&
      message.push('Debe seleccionar un jugador válido');

    if (message.length > 0) {
      setMessage(400, message);
      return;
    }

    const res: IPlayerSanctionResponse | void = await addPlayerSanction(form);
    if (res) {
      navigate(`/${RoutesNavigationViews.Match}/${match.id}`);
    }
  };

  return (
    <CustomBox>
      <Card>
        <CardContent>
          <Typography
            variant="h4"
            gutterBottom
            align="center"
            color={theme.palette.primary.main}
          >
            Crear Sanción
          </Typography>

          {errors && errors.length > 0 && (
            <>
              {errors.map((e, i) => (
                <Typography
                  key={i}
                  color="error"
                  variant="body2"
                  align="center"
                  gutterBottom
                >
                  {e}
                </Typography>
              ))}
            </>
          )}

          <TextField
            fullWidth
            select
            label="Equipo"
            name="matchSide"
            variant="outlined"
            margin="normal"
            value={selectedTeam}
            onChange={e => {
              setSelectedTeam(e.target.value as MatchSide);
              setForm(prev => ({ ...prev, playerId: GUID_EMPTY }));
            }}
          >
            {Object.values(MatchSide).map(role => (
              <MenuItem key={role} value={role}>
                {role === MatchSide.HOME
                  ? (match.homeTeam?.name ?? 'Equipo Local')
                  : (match.visitorTeam?.name ?? 'Equipo Visitante')}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            fullWidth
            margin="normal"
            label="Jugador"
            select
            disabled={!selectedTeam}
            value={form.playerId}
            onChange={e =>
              setForm(prev => ({ ...prev, playerId: e.target.value as GUID }))
            }
          >
            {availablePlayers.map(player => (
              <MenuItem key={player.id} value={player.id}>
                {player.fullName}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            fullWidth
            label="Duración"
            type="number"
            variant="outlined"
            margin="normal"
            value={form.duration}
            inputProps={{
              min: 1,
            }}
            onChange={e =>
              setForm(prev => ({ ...prev, duration: Number(e.target.value) }))
            }
          />

          <TextField
            fullWidth
            label="Descripción (opcional)"
            variant="outlined"
            margin="normal"
            value={form.description}
            onChange={e =>
              setForm(prev => ({ ...prev, description: e.target.value }))
            }
          />

          <Button
            fullWidth
            variant="contained"
            color="primary"
            onClick={handleCreate}
            sx={{ mt: 2 }}
            disabled={
              !selectedTeam ||
              form.duration <= 0 ||
              form.playerId === GUID_EMPTY
            }
          >
            Crear Sanción
          </Button>
        </CardContent>
      </Card>
    </CustomBox>
  );
};
