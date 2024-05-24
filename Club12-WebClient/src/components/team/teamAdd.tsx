import React from 'react';
import { Grid, Paper, TextField, Button, Typography } from "@mui/material";
import { useFormik } from 'formik';
import { ITeam } from "../../types/teams/team";

export const TeamAdd = ({ onTeamAdded }: { onTeamAdded: (team: ITeam) => void }) => {
    const formik = useFormik({
        initialValues: {
            name: '',
            threeLetterCode: '',
            divisionId: ''
        },
        onSubmit: async (values) => {
            const newTeam: ITeam = {
                id: '', // El ID debería ser generado por el backend
                name: values.name,
                threeLetterCode: values.threeLetterCode,
                divisionId: values.divisionId
            };

            const response = await fetch('/api/teams', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(newTeam),
            });

            if (response.ok) {
                const createdTeam = await response.json();
                onTeamAdded(createdTeam);
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
            <Typography ml={3} variant='h5'>Agregar equipo</Typography>
            <form onSubmit={formik.handleSubmit} id={"team-form"}>
                <Grid
                    container
                    spacing={2}
                    sx={{ ml: 0 }}
                    display="flex"
                    flexDirection="row"
                >
                    <Grid item  xs={12} sm={4} sx={{ m: "auto" }}>
                        <TextField
                            label="Nombre del equipo"
                            variant="outlined"
                            color="secondary"
                            fullWidth
                            id="name"
                            name="name"
                            value={formik.values.name}
                            onChange={formik.handleChange}
                        />
                    </Grid>
                    <Grid item  xs={12} sm={4} sx={{ m: "auto" }}>
                        <TextField
                            label="Codigo de 3 letras"
                            variant="outlined"
                            color="secondary"
                            fullWidth
                            id="threeLetterCode"
                            name="threeLetterCode"
                            value={formik.values.threeLetterCode}
                            onChange={formik.handleChange}
                        />
                    </Grid>
                    <Grid item xs={12} sm={4}>
                        <Button  type="submit" color="secondary" variant="contained" sx={{ mt: 2 }}>
                            Add
                        </Button>
                    </Grid>
                </Grid>
                
            </form>
        </Paper>
    );
};
