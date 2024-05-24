import React from 'react';
import { Grid, Paper, TextField, Button, Typography } from "@mui/material";
import { useFormik } from 'formik';
import { IBasePlayer } from "../../types/players/player";

export const PlayerAdd = ({ onPlayerAdded }: { onPlayerAdded: (player: IBasePlayer) => void }) => {
    const formik = useFormik({
        initialValues: {
            name: '',
            lastName: '',
            height: '',
            weight: ''
        },
        onSubmit: async (values) => {
            const newPlayer: IBasePlayer = {
                name: values.name,
                lastName: values.lastName,
                height: Number(values.height),
                weight: Number(values.weight)
            };

            const response = await fetch('/api/players', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(newPlayer),
            });

            if (response.ok) {
                const createdPlayer = await response.json();
                onPlayerAdded(createdPlayer);
            } else {
                // Manejar errores
            }
        },
    });

    return (
        <Paper
            sx={{
                p: 2,
                mb: 2,
                bgcolor: "background.paper",
                color: "text.primary",
                width: "100%",
            }}
        >   
        <Typography ml={3} variant='h5'>Agregar nuevo Jugador</Typography>
            <form onSubmit={formik.handleSubmit} id={"task-form"}>
                <Grid
                    container
                    spacing={2}
                    sx={{ ml: 0 }}
                    display="flex"
                    flexDirection="row"
                >
                    <Grid item  xs={12} sm={2} sx={{ m: "auto" }}>
                        <TextField
                            label="Nombre"
                            variant="outlined"
                            color="secondary"
                            fullWidth
                            id="name"
                            name="name"
                            value={formik.values.name}
                            onChange={formik.handleChange}
                        />
                    </Grid>
                    <Grid item xs={12} sm={2} sx={{ m: "auto" }}>
                        <TextField
                            label="Apellido"
                            variant="outlined"
                            color="secondary"
                            fullWidth
                            id="lastName"
                            name="lastName"
                            value={formik.values.lastName}
                            onChange={formik.handleChange}
                        />
                    </Grid>
                    <Grid item xs={12} sm={2} sx={{ m: "auto" }}>
                        <TextField
                            label="Altura"
                            variant="outlined"
                            color="secondary"
                            fullWidth
                            id="height"
                            name="height"
                            value={formik.values.height}
                            onChange={formik.handleChange}
                        />
                    </Grid>
                    <Grid item  xs={12} sm={2} sx={{ m: "auto" }}>
                        <TextField
                            label="Peso"
                            variant="outlined"
                            color="secondary"
                            fullWidth
                            id="weight"
                            name="weight"
                            value={formik.values.weight}
                            onChange={formik.handleChange}
                        />
                    </Grid>
                    <Grid item xs={12} sm={2}>
                        <Button type="submit" color="secondary" variant="contained">
                            Add
                        </Button>
                    </Grid>
                </Grid>
               
            </form>
        </Paper>
    );
};
